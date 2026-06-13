using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Data;

namespace AxiomRPG.ToolAPI.Tools;

public class ReadRegionTool : IToolHandler
{
    public string ToolName => "read_region";
    private readonly DataService _dataService;

    public ReadRegionTool(DataService dataService) => _dataService = dataService;

    public async Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(call.ArgumentsJson);
        if (args == null || !args.TryGetValue("regionId", out var regionId))
            return ToolResult.Error(call.CallId, "Missing regionId parameter");

        var region = await _dataService.WorldStore.LoadRegionAsync(args.GetValueOrDefault("planetId", "default"), regionId);
        if (region == null)
            return ToolResult.Error(call.CallId, $"Region '{regionId}' not found");

        var data = JsonNode.Parse(region) as JsonObject;
        return ToolResult.Ok(call.CallId, $"Region data for {regionId}", data);
    }

    public ToolDefinition GetDefinition() => new(
        "read_region",
        "Read detailed data about a region in the world. Returns biome, locations, entities, and other regional data.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["regionId"] = new JsonObject { ["type"] = "string", ["description"] = "The region ID to read" },
                ["planetId"] = new JsonObject { ["type"] = "string", ["description"] = "Planet ID (optional, uses default)" }
            },
            ["required"] = new JsonArray("regionId")
        }
    );
}
