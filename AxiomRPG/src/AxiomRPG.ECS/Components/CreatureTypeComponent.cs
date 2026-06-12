using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record CreatureTypeComponent : IComponent
{
    public string ComponentType => "creature_type";
    public string Category { get; init; } = "humanoid"; // humanoid, beast, undead, construct, etc.
    public string Race { get; init; } = "";
    public string Size { get; init; } = "medium"; // tiny, small, medium, large, huge
    public HashSet<string> Tags { get; init; } = new();
}
