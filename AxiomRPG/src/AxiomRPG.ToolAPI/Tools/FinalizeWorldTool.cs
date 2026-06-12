using System.Text.Json.Nodes;

namespace AxiomRPG.ToolAPI.Tools;

public class FinalizeWorldTool : IToolHandler
{
    public string ToolName => "finalize_world";

    public Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        // Signal that world building is complete
        return Task.FromResult(ToolResult.Ok(call.CallId, "World building finalized. Engine can now load the world."));
    }

    public ToolDefinition GetDefinition() => new(
        "finalize_world",
        "Signal that world building is complete. The engine will then load all created data and initialize the world.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject()
        }
    );
}
