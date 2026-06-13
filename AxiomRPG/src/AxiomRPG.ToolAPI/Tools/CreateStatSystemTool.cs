using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Data;
using AxiomRPG.Data.Definitions;

namespace AxiomRPG.ToolAPI.Tools;

public class CreateStatSystemTool : IToolHandler
{
    public string ToolName => "create_stat_system";
    private readonly DataService _dataService;

    public CreateStatSystemTool(DataService dataService) => _dataService = dataService;

    public Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        try
        {
            var data = JsonSerializer.Deserialize<StatSystemDefinition>(call.ArgumentsJson, _jsonOptions);
            if (data == null) return Task.FromResult(ToolResult.Error(call.CallId, "Invalid stat system data"));

            _dataService.StatSystems.SaveAsync(data).Wait();
            return Task.FromResult(ToolResult.Ok(call.CallId, $"Stat system '{data.Name}' created with {data.Stats.Count} stats and {data.CustomIndicators.Count} custom indicators"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(ToolResult.Error(call.CallId, $"JSON deserialization error: {ex.Message}"));
        }
    }

    public ToolDefinition GetDefinition() => new(
        "create_stat_system",
        "Define the stat system for the world. This includes base stats, custom indicators (like sanity, corruption, fame), and derived stat formulas. ALL entities in the game use this system.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject { ["type"] = "string" },
                ["name"] = new JsonObject { ["type"] = "string" },
                ["stats"] = new JsonObject { ["type"] = "array", ["description"] = "Array of stat definitions with name, min, max, default, category" },
                ["customIndicators"] = new JsonObject { ["type"] = "array", ["description"] = "Array of custom indicator definitions" },
                ["derivedStats"] = new JsonObject { ["type"] = "array", ["description"] = "Array of derived stat formulas" },
                ["properties"] = new JsonObject { ["type"] = "object", ["description"] = "Additional properties" }
            },
            ["required"] = new JsonArray("id", "name", "stats")
        }
    );

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
