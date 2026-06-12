using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.ToolAPI;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.AI;

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

        var responseBuilder = new StringBuilder();
        LLMResponse? finalResponse = null;

        await foreach (var chunk in LLMClient.StreamChatAsync(ConversationHistory, AvailableTools, ct))
        {
            if (chunk.ContentDelta != null) responseBuilder.Append(chunk.ContentDelta);
        }

        // Get the complete response for tool calls
        finalResponse = await LLMClient.ChatAsync(ConversationHistory, AvailableTools, ct);

        // Add assistant response to history
        ConversationHistory.Add(LLMMessage.Assistant(finalResponse.Content));

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
        var followUpBuilder = new StringBuilder();
        await foreach (var chunk in LLMClient.StreamChatAsync(ConversationHistory, AvailableTools, ct))
        {
            if (chunk.ContentDelta != null) followUpBuilder.Append(chunk.ContentDelta);
        }

        var followUpContent = followUpBuilder.ToString();
        if (!string.IsNullOrEmpty(followUpContent))
        {
            ConversationHistory.Add(LLMMessage.Assistant(followUpContent));
        }

        return new AgentResponse(followUpContent, false);
    }

    /// <summary>
    /// Stream the agent's response with tool call handling. Returns chunks as they arrive.
    /// </summary>
    public async IAsyncEnumerable<string> StreamResponseAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        ConversationHistory.Add(LLMMessage.User(userMessage));

        var fullContent = new StringBuilder();
        var toolCallAccumulators = new Dictionary<int, ToolCallAccumulatorData>();
        bool hasToolCalls = false;

        await foreach (var chunk in LLMClient.StreamChatAsync(ConversationHistory, AvailableTools, ct))
        {
            if (chunk.ContentDelta != null)
            {
                fullContent.Append(chunk.ContentDelta);
                yield return chunk.ContentDelta;
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
                    toolCallAccumulators[idx].ToolName += chunk.ToolCallDelta.ToolNameDelta;
                if (chunk.ToolCallDelta.ArgumentsDelta != null)
                    toolCallAccumulators[idx].Arguments += chunk.ToolCallDelta.ArgumentsDelta;
            }
        }

        // Process tool calls after stream completes
        if (hasToolCalls)
        {
            foreach (var (index, acc) in toolCallAccumulators.OrderBy(x => x.Key))
            {
                Logger.LogInformation("Agent {Type} calling tool: {Tool}", AgentType, acc.ToolName);

                var result = await ToolDispatcher.DispatchAsync(new ToolCall(acc.CallId, acc.ToolName, acc.Arguments));

                var resultObj = new JsonObject
                {
                    ["success"] = result.Success,
                    ["message"] = result.Message
                };
                ConversationHistory.Add(LLMMessage.ToolResult(acc.CallId, resultObj));
            }

            // Continue streaming after tool results
            await foreach (var chunk in LLMClient.StreamChatAsync(ConversationHistory, AvailableTools, ct))
            {
                if (chunk.ContentDelta != null)
                {
                    fullContent.Append(chunk.ContentDelta);
                    yield return chunk.ContentDelta;
                }
            }
        }

        var finalContent = fullContent.ToString();
        if (!string.IsNullOrEmpty(finalContent))
        {
            ConversationHistory.Add(LLMMessage.Assistant(finalContent));
        }
    }

    private class ToolCallAccumulatorData
    {
        public string CallId { get; set; } = "";
        public string ToolName { get; set; } = "";
        public string Arguments { get; set; } = "";
    }
}
