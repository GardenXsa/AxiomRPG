using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.ToolAPI;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.AI;

/// <summary>
/// Streaming event type for UI display
/// </summary>
public enum StreamEventType
{
    TextDelta,
    ToolCallStart,
    ToolCallResult,
    RoundComplete,
    Error
}

/// <summary>
/// A structured streaming event with type info for UI rendering
/// </summary>
public record StreamEvent(StreamEventType Type, string Content, string? Detail = null);

public abstract class AgentBase : IAgent
{
    protected readonly ILLMClient LLMClient;
    protected readonly ToolDispatcher ToolDispatcher;
    protected readonly IEventBus EventBus;
    protected readonly ILogger Logger;
    protected readonly List<LLMMessage> ConversationHistory = new();
    protected readonly string SystemPrompt;
    protected readonly List<ToolDefinition> AvailableTools;

    public string AgentId { get; }
    public abstract string AgentType { get; }
    public bool IsRunning { get; protected set; }

    /// <summary>
    /// Maximum number of tool-calling rounds to prevent infinite loops
    /// </summary>
    protected virtual int MaxToolRounds => 20;

    protected AgentBase(
        string agentId,
        ILLMClient llmClient,
        ToolDispatcher toolDispatcher,
        IEventBus eventBus,
        ILogger logger,
        string systemPrompt,
        List<ToolDefinition>? availableTools = null
    )
    {
        AgentId = agentId;
        LLMClient = llmClient;
        ToolDispatcher = toolDispatcher;
        EventBus = eventBus;
        Logger = logger;
        SystemPrompt = systemPrompt;
        AvailableTools = availableTools ?? toolDispatcher.GetAllToolDefinitions().ToList();
    }

    public virtual Task StartAsync(CancellationToken ct = default)
    {
        IsRunning = true;
        ConversationHistory.Clear();
        ConversationHistory.Add(LLMMessage.System(SystemPrompt));
        Logger.LogInformation("Agent {Type} ({Id}) started", AgentType, AgentId);
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken ct = default)
    {
        IsRunning = false;
        Logger.LogInformation("Agent {Type} ({Id}) stopped", AgentType, AgentId);
        return Task.CompletedTask;
    }

    public async Task<AgentResponse> SendMessageAsync(AgentMessage message, CancellationToken ct = default)
    {
        ConversationHistory.Add(LLMMessage.User(message.Content));

        // Use ChatAsync which does a single non-streaming call and collects tool calls
        var finalResponse = await LLMClient.ChatAsync(ConversationHistory, AvailableTools, ct);

        // Add assistant response to history (with tool calls if present)
        if (finalResponse.ToolCalls.Count > 0)
        {
            var toolCallInfos = finalResponse.ToolCalls
                .Select(tc => new LLMToolCallInfo(tc.CallId, tc.ToolName, tc.ArgumentsJson))
                .ToList();
            ConversationHistory.Add(LLMMessage.AssistantWithToolCalls(finalResponse.Content, toolCallInfos));
        }
        else
        {
            ConversationHistory.Add(LLMMessage.Assistant(finalResponse.Content));
        }

        // Handle tool calls
        if (finalResponse.ToolCalls.Count > 0)
        {
            return await HandleToolCallsAsync(finalResponse, ct);
        }

        return new AgentResponse(finalResponse.Content, false);
    }

    protected async Task<AgentResponse> HandleToolCallsAsync(LLMResponse response, CancellationToken ct)
    {
        foreach (var toolCall in response.ToolCalls)
        {
            Logger.LogInformation("Agent {Type} calling tool: {Tool}", AgentType, toolCall.ToolName);

            var result = await ToolDispatcher.DispatchAsync(new ToolCall(
                toolCall.CallId, toolCall.ToolName, toolCall.ArgumentsJson
            ));

            // Add tool result to conversation
            var resultObj = new JsonObject
            {
                ["success"] = result.Success,
                ["message"] = result.Message,
                ["data"] = result.Data
            };
            ConversationHistory.Add(LLMMessage.ToolResult(toolCall.CallId, resultObj));
        }

        // Let LLM continue after tool results
        var followUpResponse = await LLMClient.ChatAsync(ConversationHistory, AvailableTools, ct);

        // Add follow-up assistant response
        if (followUpResponse.ToolCalls.Count > 0)
        {
            var toolCallInfos = followUpResponse.ToolCalls
                .Select(tc => new LLMToolCallInfo(tc.CallId, tc.ToolName, tc.ArgumentsJson))
                .ToList();
            ConversationHistory.Add(LLMMessage.AssistantWithToolCalls(followUpResponse.Content, toolCallInfos));

            // Recursively handle more tool calls
            return await HandleToolCallsAsync(followUpResponse, ct);
        }
        else
        {
            if (!string.IsNullOrEmpty(followUpResponse.Content))
            {
                ConversationHistory.Add(LLMMessage.Assistant(followUpResponse.Content));
            }
        }

        return new AgentResponse(followUpResponse.Content, false);
    }

    /// <summary>
    /// Stream the agent's response with multi-round tool call handling.
    /// Returns structured StreamEvents so the UI can show tool calls, text, and progress.
    /// Loops until the AI stops making tool calls or max rounds reached.
    /// </summary>
    public async IAsyncEnumerable<StreamEvent> StreamWithEventsAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        ConversationHistory.Add(LLMMessage.User(userMessage));

        var round = 0;
        while (round < MaxToolRounds && !ct.IsCancellationRequested)
        {
            round++;
            var fullContent = new StringBuilder();
            var toolCallAccumulators = new Dictionary<int, ToolCallAccumulatorData>();
            bool hasToolCalls = false;

            await foreach (var chunk in LLMClient.StreamChatAsync(ConversationHistory, AvailableTools, ct))
            {
                if (chunk.ContentDelta != null)
                {
                    fullContent.Append(chunk.ContentDelta);
                    yield return new StreamEvent(StreamEventType.TextDelta, chunk.ContentDelta);
                }

                if (chunk.ToolCallDelta != null)
                {
                    hasToolCalls = true;
                    var idx = chunk.ToolCallDelta.Index;
                    if (!toolCallAccumulators.ContainsKey(idx))
                        toolCallAccumulators[idx] = new ToolCallAccumulatorData();

                    if (chunk.ToolCallDelta.CallId != null)
                        toolCallAccumulators[idx].CallId += chunk.ToolCallDelta.CallId;
                    if (chunk.ToolCallDelta.ToolNameDelta != null)
                    {
                        toolCallAccumulators[idx].ToolName += chunk.ToolCallDelta.ToolNameDelta;
                        // Yield tool call start event when we first see the name
                        if (toolCallAccumulators[idx].ToolName.Length == chunk.ToolCallDelta.ToolNameDelta.Length)
                        {
                            yield return new StreamEvent(StreamEventType.ToolCallStart, chunk.ToolCallDelta.ToolNameDelta);
                        }
                    }
                    if (chunk.ToolCallDelta.ArgumentsDelta != null)
                        toolCallAccumulators[idx].Arguments += chunk.ToolCallDelta.ArgumentsDelta;
                }
            }

            var textContent = fullContent.ToString();

            if (!hasToolCalls)
            {
                // No tool calls — this is the final response
                if (!string.IsNullOrEmpty(textContent))
                {
                    ConversationHistory.Add(LLMMessage.Assistant(textContent));
                }
                yield return new StreamEvent(StreamEventType.RoundComplete, $"Round {round} complete (no more tool calls)");
                yield break;
            }

            // Has tool calls — add assistant message WITH tool_calls to conversation history
            var completedToolCalls = toolCallAccumulators
                .OrderBy(x => x.Key)
                .Select(x => new LLMToolCallInfo(x.Value.CallId, x.Value.ToolName, x.Value.Arguments))
                .ToList();

            ConversationHistory.Add(LLMMessage.AssistantWithToolCalls(textContent, completedToolCalls));

            // Execute all tool calls and add results
            foreach (var (index, acc) in toolCallAccumulators.OrderBy(x => x.Key))
            {
                Logger.LogInformation("Agent {Type} calling tool: {Tool} (round {Round})", AgentType, acc.ToolName, round);

                try
                {
                    var result = await ToolDispatcher.DispatchAsync(new ToolCall(acc.CallId, acc.ToolName, acc.Arguments));

                    var resultObj = new JsonObject
                    {
                        ["success"] = result.Success,
                        ["message"] = result.Message
                    };
                    ConversationHistory.Add(LLMMessage.ToolResult(acc.CallId, resultObj));

                    var statusStr = result.Success ? "OK" : "FAILED";
                    yield return new StreamEvent(StreamEventType.ToolCallResult,
                        $"{acc.ToolName}: {result.Message}",
                        statusStr);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Tool {Tool} execution error", acc.ToolName);
                    var errorObj = new JsonObject { ["success"] = false, ["message"] = ex.Message };
                    ConversationHistory.Add(LLMMessage.ToolResult(acc.CallId, errorObj));

                    yield return new StreamEvent(StreamEventType.ToolCallResult,
                        $"{acc.ToolName}: ERROR - {ex.Message}",
                        "ERROR");
                }
            }

            yield return new StreamEvent(StreamEventType.RoundComplete, $"Round {round} complete ({completedToolCalls.Count} tool calls)");

            // Loop continues — the LLM will see the tool results and may make more calls
        }

        if (round >= MaxToolRounds)
        {
            Logger.LogWarning("Agent {Type} reached max tool rounds ({Max})", AgentType, MaxToolRounds);
            yield return new StreamEvent(StreamEventType.Error, $"Reached maximum tool rounds ({MaxToolRounds})");
        }
    }

    /// <summary>
    /// Legacy streaming method — returns raw text chunks only.
    /// Delegates to StreamWithEventsAsync for compatibility.
    /// </summary>
    public async IAsyncEnumerable<string> StreamResponseAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        await foreach (var evt in StreamWithEventsAsync(userMessage, ct))
        {
            if (evt.Type == StreamEventType.TextDelta)
                yield return evt.Content;
        }
    }

    private class ToolCallAccumulatorData
    {
        public string CallId { get; set; } = "";
        public string ToolName { get; set; } = "";
        public string Arguments { get; set; } = "";
    }
}
