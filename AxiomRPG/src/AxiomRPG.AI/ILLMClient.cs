using AxiomRPG.ToolAPI;

namespace AxiomRPG.AI;

public interface ILLMClient
{
    IAsyncEnumerable<LLMStreamChunk> StreamChatAsync(
        List<LLMMessage> messages,
        List<ToolDefinition>? tools = null,
        CancellationToken ct = default
    );

    Task<LLMResponse> ChatAsync(
        List<LLMMessage> messages,
        List<ToolDefinition>? tools = null,
        CancellationToken ct = default
    );
}

public record LLMStreamChunk(
    string? ContentDelta,
    LLMToolCallDelta? ToolCallDelta,
    bool IsComplete,
    string? FinishReason
);

public record LLMToolCallDelta(
    int Index,
    string? CallId,
    string? ToolNameDelta,
    string? ArgumentsDelta
);
