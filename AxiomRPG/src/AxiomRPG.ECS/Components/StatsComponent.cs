using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record StatsComponent : IComponent
{
    public string ComponentType => "stats";
    public Dictionary<string, float> BaseValues { get; init; } = new();
    public Dictionary<string, float> Modifiers { get; init; } = new();
    public Dictionary<string, float> CurrentValues { get; init; } = new();

    public float GetStat(string statName)
    {
        var baseVal = BaseValues.GetValueOrDefault(statName, 0f);
        var modifier = Modifiers.GetValueOrDefault(statName, 0f);
        return baseVal + modifier;
    }

    public float GetCurrent(string statName) => CurrentValues.GetValueOrDefault(statName, GetStat(statName));

    public void SetCurrent(string statName, float value) => CurrentValues[statName] = value;

    public void ModifyCurrent(string statName, float delta)
    {
        var current = GetCurrent(statName);
        CurrentValues[statName] = current + delta;
    }
}
