using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Data;
using AxiomRPG.Data.Definitions;

namespace AxiomRPG.ToolAPI.Tools;

public class CreateLocationTool : IToolHandler
{
    public string ToolName => "create_location";
    private readonly DataService _dataService;

    public CreateLocationTool(DataService dataService) => _dataService = dataService;

    public Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        try
        {
            var data = JsonSerializer.Deserialize<LocationDefinition>(call.ArgumentsJson, _jsonOptions);
            if (data == null) return Task.FromResult(ToolResult.Error(call.CallId, "Invalid location data"));

            _dataService.Locations.SaveAsync(data).Wait();
            return Task.FromResult(ToolResult.Ok(call.CallId, $"Location '{data.Name}' created with ID {data.Id}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(ToolResult.Error(call.CallId, $"JSON deserialization error: {ex.Message}"));
        }
    }

    public ToolDefinition GetDefinition() => new(
        "create_location",
        "Create a location (city, village, dungeon, cave, ruins, camp). Locations span multiple chunks with internal structure.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject { ["type"] = "string" },
                ["name"] = new JsonObject { ["type"] = "string" },
                ["description"] = new JsonObject { ["type"] = "string" },
                ["type"] = new JsonObject { ["type"] = "string", ["description"] = "city, village, dungeon, cave, ruins, camp" },
                ["biomeId"] = new JsonObject { ["type"] = "string" },
                ["districts"] = new JsonObject { ["type"] = "array", ["description"] = "List of district IDs" },
                ["structures"] = new JsonObject { ["type"] = "array", ["description"] = "List of structure IDs" },
                ["connections"] = new JsonObject { ["type"] = "array", ["description"] = "List of connected location IDs" },
                ["sizeX"] = new JsonObject { ["type"] = "integer" },
                ["sizeY"] = new JsonObject { ["type"] = "integer" },
                ["properties"] = new JsonObject { ["type"] = "object", ["description"] = "Additional properties" }
            },
            ["required"] = new JsonArray("id", "name", "type")
        }
    );

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
