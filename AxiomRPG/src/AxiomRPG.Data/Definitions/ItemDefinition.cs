namespace AxiomRPG.Data.Definitions;

public record ItemDefinition(
    string Id,
    string Name,
    string Description,
    string ItemType,
    char AsciiChar,
    string ForegroundColor,
    float Weight,
    float Value,
    bool IsStackable,
    int MaxStack,
    Dictionary<string, float> Effects,
    Dictionary<string, string> Properties
);
