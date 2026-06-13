namespace AxiomRPG.Data.Definitions;

public record StatSystemDefinition(
    string Id,
    string Name,
    List<StatDefinition> Stats,
    List<CustomIndicatorDefinition> CustomIndicators,
    List<DerivedStatFormula> DerivedStats,
    Dictionary<string, string> Properties
);

public record StatDefinition(
    string Name,
    string DisplayName,
    string Description,
    float MinValue,
    float MaxValue,
    float DefaultValue,
    string Category
);

public record CustomIndicatorDefinition(
    string Name,
    string DisplayName,
    string Description,
    float MinValue,
    float MaxValue,
    float DefaultValue,
    string DecayRate,
    string AffectedBy
);

public record DerivedStatFormula(
    string Name,
    string DisplayName,
    string Formula,
    List<string> Dependencies
);
