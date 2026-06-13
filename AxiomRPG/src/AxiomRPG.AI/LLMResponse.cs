namespace AxiomRPG.AI;

public record LLMResponse(
    string Content,
    List<LLMToolCall> ToolCalls,
    bool IsComplete,
    string FinishReason
);

public record LLMToolCall(
    string CallId,
    string ToolName,
    string ArgumentsJson
);
