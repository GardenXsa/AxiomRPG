using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record AIComponent : IComponent
{
    public string ComponentType => "ai";
    public string BehaviorType { get; init; } = "idle"; // idle, wander, patrol, aggressive, flee, pack_predator
    public string? FactionId { get; init; }
    public float AggroRange { get; init; } = 5f;
    public float DetectionRange { get; init; } = 10f;
    public List<string> HostileToFactions { get; init; } = new();
    public List<string> AlliedToFactions { get; init; } = new();
    public string? PatrolRouteId { get; init; }
    public string? CurrentGoal { get; set; }
}
