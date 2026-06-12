using System.Text.Json.Nodes;

namespace AxiomRPG.Core.Interfaces;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonNode? ParameterSchema { get; }
    Task<ToolResult> ExecuteAsync(JsonObject parameters);
}

public record ToolResult(bool Success, string Message, JsonNode? Data = null);
