using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Data;
using AxiomRPG.Data.Definitions;

namespace AxiomRPG.ToolAPI.Tools;

public class CreateItemTool : IToolHandler
{
    public string ToolName => "create_item";
    private readonly DataService _dataService;

    public CreateItemTool(DataService dataService) => _dataService = dataService;

    public Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        try
        {
            var data = JsonSerializer.Deserialize<ItemDefinition>(call.ArgumentsJson, _jsonOptions);
            if (data == null) return Task.FromResult(ToolResult.Error(call.CallId, "Invalid item data"));

            _dataService.Items.SaveAsync(data).Wait();
            return Task.FromResult(ToolResult.Ok(call.CallId, $"Item '{data.Name}' created with ID {data.Id}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(ToolResult.Error(call.CallId, $"JSON deserialization error: {ex.Message}"));
        }
    }

    public ToolDefinition GetDefinition() => new(
        "create_item",
        "Create an item definition. Items can be weapons, armor, consumables, quest items, or miscellaneous objects.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject { ["type"] = "string" },
                ["name"] = new JsonObject { ["type"] = "string" },
                ["description"] = new JsonObject { ["type"] = "string" },
                ["itemType"] = new JsonObject { ["type"] = "string", ["description"] = "weapon, armor, consumable, misc, quest_item" },
                ["asciiChar"] = new JsonObject { ["type"] = "string", ["description"] = "ASCII character for rendering" },
                ["foregroundColor"] = new JsonObject { ["type"] = "string", ["description"] = "Hex foreground color" },
                ["weight"] = new JsonObject { ["type"] = "number" },
                ["value"] = new JsonObject { ["type"] = "number" },
                ["isStackable"] = new JsonObject { ["type"] = "boolean" },
                ["maxStack"] = new JsonObject { ["type"] = "integer" },
                ["effects"] = new JsonObject { ["type"] = "object", ["description"] = "Effect name to value mapping" },
                ["properties"] = new JsonObject { ["type"] = "object", ["description"] = "Additional properties" }
            },
            ["required"] = new JsonArray("id", "name", "itemType")
        }
    );

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
