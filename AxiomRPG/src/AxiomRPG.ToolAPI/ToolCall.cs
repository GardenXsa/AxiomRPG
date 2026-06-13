namespace AxiomRPG.ToolAPI;

public record ToolCall(
    string CallId,
    string ToolName,
    string ArgumentsJson
);
