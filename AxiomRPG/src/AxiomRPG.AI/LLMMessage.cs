using System.Text.Json.Nodes;

namespace AxiomRPG.AI;

public record LLMMessage(
    string Role,
    string? Content = null,
    string? ToolCallId = null,
    string? ToolName = null,
    JsonObject? ToolArguments = null,
    JsonObject? ToolResultData = null,
    List<LLMToolCallInfo>? ToolCalls = null
)
{
    public static LLMMessage System(string content) => new("system", content);
    public static LLMMessage User(string content) => new("user", content);
    public static LLMMessage Assistant(string content) => new("assistant", content);
    public static LLMMessage AssistantWithToolCalls(string content, List<LLMToolCallInfo> toolCalls) =>
        new("assistant", content == "" ? null : content, ToolCalls: toolCalls);
    public static LLMMessage ToolCall(string callId, string toolName, JsonObject arguments) =>
        new("assistant", null, callId, toolName, arguments);
    public static LLMMessage ToolResult(string callId, JsonObject result) =>
        new("tool", null, callId, ToolResultData: result);
}

/// <summary>
/// Represents a completed tool call in an assistant message.
/// Used for serializing tool_calls array in OpenAI API format.
/// </summary>
public record LLMToolCallInfo(
    string CallId,
    string ToolName,
    string ArgumentsJson
);
