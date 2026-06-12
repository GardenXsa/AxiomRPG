using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Data;
using AxiomRPG.Data.Definitions;

namespace AxiomRPG.ToolAPI.Tools;

public class CreateFactionTool : IToolHandler
{
    public string ToolName => "create_faction";
    private readonly DataService _dataService;

    public CreateFactionTool(DataService dataService) => _dataService = dataService;

    public Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        try
        {
            var data = JsonSerializer.Deserialize<FactionDefinition>(call.ArgumentsJson, _jsonOptions);
            if (data == null) return Task.FromResult(ToolResult.Error(call.CallId, "Invalid faction data"));

            _dataService.Factions.SaveAsync(data).Wait();
            return Task.FromResult(ToolResult.Ok(call.CallId, $"Faction '{data.Name}' created with ID {data.Id}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(ToolResult.Error(call.CallId, $"JSON deserialization error: {ex.Message}"));
        }
    }

    public ToolDefinition GetDefinition() => new(
        "create_faction",
        "Create a faction with default relations to other factions.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject { ["type"] = "string" },
                ["name"] = new JsonObject { ["type"] = "string" },
                ["description"] = new JsonObject { ["type"] = "string" },
                ["territory"] = new JsonObject { ["type"] = "string" },
                ["defaultRelations"] = new JsonObject { ["type"] = "object", ["description"] = "Faction ID to relation score mapping" },
                ["allyFactions"] = new JsonObject { ["type"] = "array", ["description"] = "List of allied faction IDs" },
                ["enemyFactions"] = new JsonObject { ["type"] = "array", ["description"] = "List of enemy faction IDs" },
                ["properties"] = new JsonObject { ["type"] = "object", ["description"] = "Additional properties" }
            },
            ["required"] = new JsonArray("id", "name")
        }
    );

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
