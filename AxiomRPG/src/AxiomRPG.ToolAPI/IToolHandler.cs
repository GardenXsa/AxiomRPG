using System.Text.Json.Nodes;

namespace AxiomRPG.ToolAPI;

public interface IToolHandler
{
    string ToolName { get; }
    Task<ToolResult> ExecuteAsync(ToolCall call);
    ToolDefinition GetDefinition();
}
