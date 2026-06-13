using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Data;
using AxiomRPG.Data.Definitions;

namespace AxiomRPG.ToolAPI.Tools;

public class CreateCreatureTemplateTool : IToolHandler
{
    public string ToolName => "create_creature_template";
    private readonly DataService _dataService;

    public CreateCreatureTemplateTool(DataService dataService) => _dataService = dataService;

    public Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        try
        {
            var data = JsonSerializer.Deserialize<CreatureTemplateDefinition>(call.ArgumentsJson, _jsonOptions);
            if (data == null) return Task.FromResult(ToolResult.Error(call.CallId, "Invalid creature template data"));

            _dataService.Creatures.SaveAsync(data).Wait();
            return Task.FromResult(ToolResult.Ok(call.CallId, $"Creature template '{data.Name}' created with ID {data.Id}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(ToolResult.Error(call.CallId, $"JSON deserialization error: {ex.Message}"));
        }
    }

    public ToolDefinition GetDefinition() => new(
        "create_creature_template",
        "Create a creature template. All creatures (NPCs, monsters, animals) are defined from templates with stats, behavior, and appearance.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject { ["type"] = "string", ["description"] = "Unique template ID" },
                ["name"] = new JsonObject { ["type"] = "string", ["description"] = "Creature name" },
                ["description"] = new JsonObject { ["type"] = "string", ["description"] = "Detailed description" },
                ["category"] = new JsonObject { ["type"] = "string", ["description"] = "humanoid, beast, undead, construct, etc." },
                ["race"] = new JsonObject { ["type"] = "string", ["description"] = "Race identifier" },
                ["size"] = new JsonObject { ["type"] = "string", ["description"] = "tiny, small, medium, large, huge" },
                ["defaultBehavior"] = new JsonObject { ["type"] = "string", ["description"] = "idle, wander, patrol, aggressive, flee, pack_predator" },
                ["asciiChar"] = new JsonObject { ["type"] = "string", ["description"] = "Single ASCII character to represent this creature" },
                ["foregroundColor"] = new JsonObject { ["type"] = "string", ["description"] = "Hex color for the ASCII character" },
                ["backgroundColor"] = new JsonObject { ["type"] = "string", ["description"] = "Hex background color for the ASCII character" },
                ["baseStats"] = new JsonObject { ["type"] = "object", ["description"] = "Base stat values (name to value mapping)" },
                ["abilities"] = new JsonObject { ["type"] = "array", ["description"] = "List of ability IDs" },
                ["naturalDrops"] = new JsonObject { ["type"] = "array", ["description"] = "List of natural drop item IDs" },
                ["factionId"] = new JsonObject { ["type"] = "string", ["description"] = "Faction this creature belongs to" },
                ["properties"] = new JsonObject { ["type"] = "object", ["description"] = "Additional properties" }
            },
            ["required"] = new JsonArray("id", "name", "category")
        }
    );

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
