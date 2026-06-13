namespace AxiomRPG.Data.Definitions;

public record RaceDefinition(
    string Id,
    string Name,
    string Description,
    string Category,
    string Size,
    Dictionary<string, float> BaseStats,
    List<string> RacialAbilities,
    List<string> AllowedClasses,
    Dictionary<string, string> Properties
);
