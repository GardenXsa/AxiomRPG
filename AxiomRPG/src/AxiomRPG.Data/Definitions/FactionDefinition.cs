namespace AxiomRPG.Data.Definitions;

public record FactionDefinition(
    string Id,
    string Name,
    string Description,
    string Territory,
    Dictionary<string, int> DefaultRelations,
    List<string> AllyFactions,
    List<string> EnemyFactions,
    Dictionary<string, string> Properties
);
