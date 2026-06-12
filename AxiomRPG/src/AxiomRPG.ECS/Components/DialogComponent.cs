using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record DialogComponent : IComponent
{
    public string ComponentType => "dialog";
    public string Personality { get; init; } = "";
    public string SpeakingStyle { get; init; } = "neutral";
    public Dictionary<string, string> KnownTopics { get; init; } = new();
    public HashSet<string> DialogFlags { get; init; } = new();
    public Dictionary<string, float> RelationsWith { get; init; } = new(); // entity id -> relation value (-100 to 100)
}
