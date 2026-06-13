using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Components;
using AxiomRPG.Core.Events;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.Core.Types;
using AxiomRPG.Data;
using AxiomRPG.ECS;

namespace AxiomRPG.ToolAPI.Tools;

public class GiveItemTool : IToolHandler
{
    public string ToolName => "give_item";
    private readonly EntityManager _entityManager;
    private readonly DataService _dataService;
    private readonly IEventBus _eventBus;

    public GiveItemTool(EntityManager entityManager, DataService dataService, IEventBus eventBus)
    {
        _entityManager = entityManager;
        _dataService = dataService;
        _eventBus = eventBus;
    }

    public async Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        var args = JsonSerializer.Deserialize<GiveItemArgs>(call.ArgumentsJson);
        if (args == null) return ToolResult.Error(call.CallId, "Invalid arguments");

        // Create item entity
        var itemEntity = _entityManager.CreateEntity(EntityId.New());

        var itemDef = await _dataService.Items.GetByIdAsync(args.ItemDefinitionId);
        if (itemDef != null)
        {
            _entityManager.AddComponent(itemEntity.Id, new ItemComponent
            {
                ItemType = itemDef.ItemType,
                Weight = itemDef.Weight,
                Value = itemDef.Value,
                IsStackable = itemDef.IsStackable,
                MaxStack = itemDef.MaxStack,
                Effects = itemDef.Effects,
                Description = itemDef.Description
            });
            _entityManager.AddComponent(itemEntity.Id, new RenderableComponent
            {
                Tile = new AsciiTile(itemDef.AsciiChar, itemDef.ForegroundColor, "#000000"),
                RenderLayer = 1
            });
        }

        // Add to target's inventory
        var target = _entityManager.GetEntity(EntityId.From(args.TargetEntityId));
        if (target != null)
        {
            var inventory = _entityManager.GetComponent<InventoryComponent>(target.Id);
            if (inventory != null && !inventory.IsFull)
            {
                inventory.Slots.Add(new InventorySlot(itemEntity.Id.Value, args.Quantity ?? 1));
            }
        }

        await _eventBus.PublishAsync(new ItemGivenEvent(itemEntity.Id.Value, args.TargetEntityId, null));
        return ToolResult.Ok(call.CallId, $"Item '{args.ItemDefinitionId}' (x{args.Quantity ?? 1}) given to {args.TargetEntityId}");
    }

    public ToolDefinition GetDefinition() => new(
        "give_item",
        "Give an item to a player or NPC. Creates an item entity from a definition and adds it to the target's inventory.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["itemDefinitionId"] = new JsonObject { ["type"] = "string", ["description"] = "The item definition ID" },
                ["targetEntityId"] = new JsonObject { ["type"] = "string", ["description"] = "Entity ID to give the item to" },
                ["quantity"] = new JsonObject { ["type"] = "integer", ["description"] = "Number of items (default 1)" }
            },
            ["required"] = new JsonArray("itemDefinitionId", "targetEntityId")
        }
    );

    private record GiveItemArgs(string ItemDefinitionId, string TargetEntityId, int? Quantity);
}
