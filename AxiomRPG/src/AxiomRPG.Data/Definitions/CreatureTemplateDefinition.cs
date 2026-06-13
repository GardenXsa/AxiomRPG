namespace AxiomRPG.Data.Definitions;

public record CreatureTemplateDefinition(
    string Id,
    string Name,
    string Description,
    string Category,      // humanoid, beast, undead, construct
    string Race,
    string Size,          // tiny, small, medium, large, huge
    string DefaultBehavior,
    char AsciiChar,
    string ForegroundColor,
    string BackgroundColor,
    Dictionary<string, float> BaseStats,
    List<string> Abilities,
    List<string> NaturalDrops,
    string? FactionId,
    Dictionary<string, string> Properties
);
