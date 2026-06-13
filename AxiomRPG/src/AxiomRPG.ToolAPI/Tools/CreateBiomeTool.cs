using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Data;
using AxiomRPG.Data.Definitions;

namespace AxiomRPG.ToolAPI.Tools;

public class CreateBiomeTool : IToolHandler
{
    public string ToolName => "create_biome";
    private readonly DataService _dataService;

    public CreateBiomeTool(DataService dataService) => _dataService = dataService;

    public Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        try
        {
            var data = JsonSerializer.Deserialize<BiomeDefinition>(call.ArgumentsJson, _jsonOptions);
            if (data == null) return Task.FromResult(ToolResult.Error(call.CallId, "Invalid biome data"));

            _dataService.Biomes.SaveAsync(data).Wait();
            return Task.FromResult(ToolResult.Ok(call.CallId, $"Biome '{data.Name}' created with ID {data.Id}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(ToolResult.Error(call.CallId, $"JSON deserialization error: {ex.Message}"));
        }
    }

    public ToolDefinition GetDefinition() => new(
        "create_biome",
        "Create a new biome type for the world. Biomes define terrain types, flora, fauna, and weather rules.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject { ["type"] = "string", ["description"] = "Unique biome identifier" },
                ["name"] = new JsonObject { ["type"] = "string", ["description"] = "Display name of the biome" },
                ["description"] = new JsonObject { ["type"] = "string", ["description"] = "Detailed biome description" },
                ["defaultTile"] = new JsonObject { ["type"] = "string", ["description"] = "Default tile type for this biome" },
                ["tileWeights"] = new JsonObject { ["type"] = "object", ["description"] = "Tile type to weight mapping" },
                ["flora"] = new JsonObject { ["type"] = "array", ["description"] = "List of flora types" },
                ["fauna"] = new JsonObject { ["type"] = "array", ["description"] = "List of fauna types" },
                ["weatherRules"] = new JsonObject { ["type"] = "string", ["description"] = "Weather rules identifier" },
                ["temperatureMin"] = new JsonObject { ["type"] = "number", ["description"] = "Minimum temperature" },
                ["temperatureMax"] = new JsonObject { ["type"] = "number", ["description"] = "Maximum temperature" },
                ["rainfall"] = new JsonObject { ["type"] = "number", ["description"] = "Average rainfall" },
                ["allowedStructures"] = new JsonObject { ["type"] = "array", ["description"] = "Allowed structure types" },
                ["properties"] = new JsonObject { ["type"] = "object", ["description"] = "Additional properties" }
            },
            ["required"] = new JsonArray("id", "name", "description")
        }
    );

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
