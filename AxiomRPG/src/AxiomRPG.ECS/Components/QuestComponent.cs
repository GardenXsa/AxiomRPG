using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record QuestComponent : IComponent
{
    public string ComponentType => "quest";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public List<QuestObjective> Objectives { get; init; } = new();
    public string? AssignedTo { get; init; } // entity id
    public QuestStatus Status { get; set; } = QuestStatus.Inactive;
    public List<string> Rewards { get; init; } = new(); // item entity ids
}

public record QuestObjective(string Id, string Description, QuestObjectiveType Type, string? TargetId = null, int Required = 1, int Current = 0);

public enum QuestStatus { Inactive, Active, Completed, Failed }
public enum QuestObjectiveType { Kill, Fetch, Talk, Explore, Custom }
