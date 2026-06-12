using System.Text.Json.Nodes;

namespace AxiomRPG.AI;

public record LLMMessage(
    string Role,
    string? Content = null,
    string? ToolCallId = null,
    string? ToolName = null,
    JsonObject? ToolArguments = null,
    JsonObject? ToolResultData = null
)
{
    public static LLMMessage System(string content) => new("system", content);
    public static LLMMessage User(string content) => new("user", content);
    public static LLMMessage Assistant(string content) => new("assistant", content);
    public static LLMMessage ToolCall(string callId, string toolName, JsonObject arguments) =>
        new("assistant", null, callId, toolName, arguments);
    public static LLMMessage ToolResult(string callId, JsonObject result) =>
        new("tool", null, callId, ToolResultData: result);
}
