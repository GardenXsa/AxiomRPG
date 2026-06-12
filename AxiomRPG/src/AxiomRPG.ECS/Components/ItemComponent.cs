using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record ItemComponent : IComponent
{
    public string ComponentType => "item";
    public string ItemType { get; init; } = "misc"; // weapon, armor, consumable, misc, quest_item
    public float Weight { get; init; } = 0.1f;
    public float Value { get; init; } = 0f;
    public bool IsStackable { get; init; } = false;
    public int MaxStack { get; init; } = 1;
    public Dictionary<string, float> Effects { get; init; } = new(); // e.g., {"damage": 5, "heal": 0}
    public string? Description { get; init; }
}
