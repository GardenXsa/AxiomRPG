using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record FactionComponent : IComponent
{
    public string ComponentType => "faction";
    public string FactionId { get; init; } = "";
    public string FactionName { get; init; } = "";
    public Dictionary<string, int> Relations { get; init; } = new(); // faction id -> relation (-100 to 100)
}
