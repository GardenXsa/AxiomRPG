using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record HealthComponent : IComponent
{
    public string ComponentType => "health";
    public float MaxHp { get; init; }
    public float CurrentHp { get; set; }
    public float MaxStamina { get; init; }
    public float CurrentStamina { get; set; }
    public bool IsDead => CurrentHp <= 0;
    public bool IsUnconscious => CurrentHp > 0 && CurrentHp <= MaxHp * 0.1f;
}
