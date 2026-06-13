using System.Text.Json.Nodes;

namespace AxiomRPG.ToolAPI;

public record ToolResult(
    string CallId,
    bool Success,
    string Message,
    JsonObject? Data = null
)
{
    public static ToolResult Ok(string callId, string message, JsonObject? data = null) =>
        new(callId, true, message, data);

    public static ToolResult Error(string callId, string message) =>
        new(callId, false, message);
}
