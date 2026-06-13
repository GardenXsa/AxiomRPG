using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.ToolAPI;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.AI;

public class OpenAIClient : ILLMClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AIClientConfiguration _config;
    private readonly ILogger<OpenAIClient> _logger;
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = {
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    };

    public OpenAIClient(AIClientConfiguration config, ILogger<OpenAIClient> logger)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(120) // 2 min timeout for long tool-call responses
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        if (!string.IsNullOrEmpty(config.OrganizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", config.OrganizationId);
        }
    }

    public async IAsyncEnumerable<LLMStreamChunk> StreamChatAsync(
        List<LLMMessage> messages,
        List<ToolDefinition>? tools = null,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var requestBody = BuildRequestBody(messages, tools, stream: true);

        HttpResponseMessage? response = null;
        Exception? lastException = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (ct.IsCancellationRequested) yield break;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
                {
                    Content = JsonContent.Create(requestBody)
                };

                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
                break; // Success — exit retry loop
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                lastException = ex;
                _logger.LogWarning(ex, "HTTP request failed (attempt {Attempt}/{Max}), retrying in {Delay}s...",
                    attempt + 1, MaxRetries + 1, RetryDelays[attempt].TotalSeconds);

                response?.Dispose();
                response = null;

                await Task.Delay(RetryDelays[attempt], ct);
            }
            catch (TaskCanceledException ex) when (attempt < MaxRetries && !ct.IsCancellationRequested)
            {
                // Timeout, not user cancellation
                lastException = ex;
                _logger.LogWarning(ex, "HTTP request timed out (attempt {Attempt}/{Max}), retrying in {Delay}s...",
                    attempt + 1, MaxRetries + 1, RetryDelays[attempt].TotalSeconds);

                response?.Dispose();
                response = null;

                await Task.Delay(RetryDelays[attempt], ct);
            }
        }

        if (response == null)
        {
            _logger.LogError(lastException, "All {Max} retries exhausted for StreamChatAsync", MaxRetries + 1);
            throw lastException ?? new HttpRequestException("Failed to connect after retries");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var toolCallAccumulators = new Dictionary<int, ToolCallAccumulator>();

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;
            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            LLMStreamChunk? streamChunk = null;
            try
            {
                var chunk = JsonNode.Parse(data)?.AsObject();
                if (chunk == null) continue;

                var choice = chunk["choices"]?[0]?.AsObject();
                if (choice == null) continue;

                var delta = choice["delta"]?.AsObject();
                var finishReason = choice["finish_reason"]?.GetValue<string>();

                string? contentDelta = delta?["content"]?.GetValue<string>();

                // Handle tool call deltas
                LLMToolCallDelta? toolCallDelta = null;
                var toolCallsArray = delta?["tool_calls"]?.AsArray();
                if (toolCallsArray != null)
                {
                    foreach (var tcNode in toolCallsArray)
                    {
                        var tc = tcNode?.AsObject();
                        if (tc == null) continue;

                        var index = tc["index"]?.GetValue<int>() ?? 0;
                        var callId = tc["id"]?.GetValue<string>();
                        var func = tc["function"]?.AsObject();
                        var nameDelta = func?["name"]?.GetValue<string>();
                        var argsDelta = func?["arguments"]?.GetValue<string>();

                        if (!toolCallAccumulators.ContainsKey(index))
                            toolCallAccumulators[index] = new ToolCallAccumulator();

                        if (callId != null) toolCallAccumulators[index].CallId += callId;
                        if (nameDelta != null) toolCallAccumulators[index].ToolName += nameDelta;
                        if (argsDelta != null) toolCallAccumulators[index].Arguments += argsDelta;

                        toolCallDelta = new LLMToolCallDelta(index, callId, nameDelta, argsDelta);
                    }
                }

                streamChunk = new LLMStreamChunk(
                    contentDelta,
                    toolCallDelta,
                    finishReason != null,
                    finishReason
                );
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse SSE chunk");
            }

            if (streamChunk != null)
                yield return streamChunk;
        }

        response.Dispose();
    }

    public async Task<LLMResponse> ChatAsync(
        List<LLMMessage> messages,
        List<ToolDefinition>? tools = null,
        CancellationToken ct = default
    )
    {
        var fullContent = new StringBuilder();
        var toolCalls = new List<LLMToolCall>();
        var toolCallAccumulators = new Dictionary<int, ToolCallAccumulator>();

        await foreach (var chunk in StreamChatAsync(messages, tools, ct))
        {
            if (chunk.ContentDelta != null) fullContent.Append(chunk.ContentDelta);

            if (chunk.ToolCallDelta != null)
            {
                var idx = chunk.ToolCallDelta.Index;
                if (!toolCallAccumulators.ContainsKey(idx))
                    toolCallAccumulators[idx] = new ToolCallAccumulator();

                if (chunk.ToolCallDelta.CallId != null) toolCallAccumulators[idx].CallId += chunk.ToolCallDelta.CallId;
                if (chunk.ToolCallDelta.ToolNameDelta != null) toolCallAccumulators[idx].ToolName += chunk.ToolCallDelta.ToolNameDelta;
                if (chunk.ToolCallDelta.ArgumentsDelta != null) toolCallAccumulators[idx].Arguments += chunk.ToolCallDelta.ArgumentsDelta;
            }
        }

        // Build tool calls from accumulators
        foreach (var (index, acc) in toolCallAccumulators.OrderBy(x => x.Key))
        {
            toolCalls.Add(new LLMToolCall(acc.CallId, acc.ToolName, acc.Arguments));
        }

        return new LLMResponse(fullContent.ToString(), toolCalls, true,
            toolCalls.Count > 0 ? "tool_calls" : "stop");
    }

    private JsonObject BuildRequestBody(List<LLMMessage> messages, List<ToolDefinition>? tools, bool stream)
    {
        var messagesArray = new JsonArray();

        foreach (var msg in messages)
        {
            var msgObj = new JsonObject { ["role"] = msg.Role };

            if (msg.Role == "tool")
            {
                // Tool result message
                msgObj["tool_call_id"] = msg.ToolCallId;
                msgObj["content"] = msg.ToolResultData?.ToJsonString() ?? "";
            }
            else if (msg.Role == "assistant" && msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                // Assistant message with tool_calls
                if (msg.Content != null)
                    msgObj["content"] = msg.Content;
                else
                    msgObj["content"] = NullNode;

                var toolCallsArray = new JsonArray();
                foreach (var tc in msg.ToolCalls)
                {
                    toolCallsArray.Add(new JsonObject
                    {
                        ["id"] = tc.CallId,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = tc.ToolName,
                            ["arguments"] = tc.ArgumentsJson
                        }
                    });
                }
                msgObj["tool_calls"] = toolCallsArray;
            }
            else
            {
                msgObj["content"] = msg.Content ?? "";
            }

            messagesArray.Add(msgObj);
        }

        var body = new JsonObject
        {
            ["model"] = _config.Model,
            ["stream"] = stream,
            ["max_tokens"] = _config.MaxTokens,
            ["temperature"] = _config.Temperature,
            ["messages"] = messagesArray
        };

        if (tools != null && tools.Count > 0)
        {
            body["tools"] = new JsonArray(tools.Select(t => t.ToOpenAIToolFormat()).ToArray<JsonNode>());
        }

        return body;
    }

    /// <summary>
    /// JSON null literal node for OpenAI API compatibility
    /// </summary>
    private static JsonNode NullNode => JsonNode.Parse("null")!;

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private class ToolCallAccumulator
    {
        public string CallId { get; set; } = "";
        public string ToolName { get; set; } = "";
        public string Arguments { get; set; } = "";
    }
}
