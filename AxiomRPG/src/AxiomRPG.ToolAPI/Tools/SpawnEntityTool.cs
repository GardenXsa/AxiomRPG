using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Components;
using AxiomRPG.Core.Events;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.Core.Types;
using AxiomRPG.Data;
using AxiomRPG.ECS;

namespace AxiomRPG.ToolAPI.Tools;

public class SpawnEntityTool : IToolHandler
{
    public string ToolName => "spawn_entity";
    private readonly EntityManager _entityManager;
    private readonly DataService _dataService;
    private readonly IEventBus _eventBus;

    public SpawnEntityTool(EntityManager entityManager, DataService dataService, IEventBus eventBus)
    {
        _entityManager = entityManager;
        _dataService = dataService;
        _eventBus = eventBus;
    }

    public async Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        var args = JsonSerializer.Deserialize<SpawnEntityArgs>(call.ArgumentsJson);
        if (args == null) return ToolResult.Error(call.CallId, "Invalid spawn arguments");

        var entity = _entityManager.CreateEntity(EntityId.New());

        // Add position
        _entityManager.AddComponent(entity.Id, new PositionComponent(
            args.X, args.Y, args.RegionId, args.ZoneId, args.ChunkId, args.Z ?? 0
        ));

        // If template specified, load template and add components
        if (!string.IsNullOrEmpty(args.TemplateId))
        {
            var template = await _dataService.Creatures.GetByIdAsync(args.TemplateId);
            if (template != null)
            {
                _entityManager.AddComponent(entity.Id, new CreatureTypeComponent
                {
                    Category = template.Category,
                    Race = template.Race,
                    Size = template.Size
                });
                _entityManager.AddComponent(entity.Id, new RenderableComponent
                {
                    Tile = new AsciiTile(template.AsciiChar, template.ForegroundColor, template.BackgroundColor),
                    RenderLayer = 2
                });
                _entityManager.AddComponent(entity.Id, new AIComponent
                {
                    BehaviorType = template.DefaultBehavior,
                    FactionId = template.FactionId
                });
            }
        }

        await _eventBus.PublishAsync(new EntitySpawnedEvent(entity.Id.Value, args.TemplateId ?? "custom", args.X, args.Y));
        return ToolResult.Ok(call.CallId, $"Entity spawned with ID {entity.Id.Value} at ({args.X}, {args.Y})");
    }

    public ToolDefinition GetDefinition() => new(
        "spawn_entity",
        "Spawn an entity in the world at a specific location. Can spawn from a template or as a custom entity.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["templateId"] = new JsonObject { ["type"] = "string", ["description"] = "Creature template ID to spawn from" },
                ["x"] = new JsonObject { ["type"] = "integer", ["description"] = "X position" },
                ["y"] = new JsonObject { ["type"] = "integer", ["description"] = "Y position" },
                ["regionId"] = new JsonObject { ["type"] = "string", ["description"] = "Region to spawn in" },
                ["zoneId"] = new JsonObject { ["type"] = "string", ["description"] = "Zone within region (optional)" },
                ["chunkId"] = new JsonObject { ["type"] = "string", ["description"] = "Chunk within zone (optional)" },
                ["z"] = new JsonObject { ["type"] = "integer", ["description"] = "Z-level (0 = ground, -1 = basement, 1 = second floor)" }
            },
            ["required"] = new JsonArray("x", "y", "regionId")
        }
    );

    private record SpawnEntityArgs(string? TemplateId, int X, int Y, string RegionId, string? ZoneId, string? ChunkId, int? Z);
}
