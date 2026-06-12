using System.Text.Json;
using System.Text.Json.Nodes;
using AxiomRPG.Components;
using AxiomRPG.Core.Events;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.Core.Types;
using AxiomRPG.ECS;

namespace AxiomRPG.ToolAPI.Tools;

public class CreateQuestTool : IToolHandler
{
    public string ToolName => "create_quest";
    private readonly EntityManager _entityManager;
    private readonly IEventBus _eventBus;

    public CreateQuestTool(EntityManager entityManager, IEventBus eventBus)
    {
        _entityManager = entityManager;
        _eventBus = eventBus;
    }

    public async Task<ToolResult> ExecuteAsync(ToolCall call)
    {
        var args = JsonSerializer.Deserialize<CreateQuestArgs>(call.ArgumentsJson);
        if (args == null) return ToolResult.Error(call.CallId, "Invalid quest data");

        var questEntity = _entityManager.CreateEntity(EntityId.New());
        var questId = new QuestId(questEntity.Id.Value);

        var quest = new QuestComponent
        {
            Title = args.Title,
            Description = args.Description,
            AssignedTo = args.AssignedTo,
            Status = QuestStatus.Active,
            Objectives = args.Objectives.Select((o, i) => new QuestObjective(
                $"obj_{i}", o.Description, o.Type, o.TargetId, o.Required, 0
            )).ToList()
        };

        _entityManager.AddComponent(questEntity.Id, quest);

        await _eventBus.PublishAsync(new QuestCreatedEvent(questId.Value, args.Title, args.AssignedTo ?? "unknown"));
        return ToolResult.Ok(call.CallId, $"Quest '{args.Title}' created and assigned to {args.AssignedTo}");
    }

    public ToolDefinition GetDefinition() => new(
        "create_quest",
        "Create a new quest and optionally assign it to a player or NPC.",
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["title"] = new JsonObject { ["type"] = "string", ["description"] = "Quest title" },
                ["description"] = new JsonObject { ["type"] = "string", ["description"] = "Detailed quest description" },
                ["assignedTo"] = new JsonObject { ["type"] = "string", ["description"] = "Entity ID to assign the quest to" },
                ["objectives"] = new JsonObject { ["type"] = "array", ["description"] = "Array of quest objectives" }
            },
            ["required"] = new JsonArray("title", "description")
        }
    );

    private record CreateQuestArgs(string Title, string Description, string? AssignedTo, List<QuestObjectiveArgs> Objectives);
    private record QuestObjectiveArgs(string Description, QuestObjectiveType Type, string? TargetId, int Required = 1);
}
