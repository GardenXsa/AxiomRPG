namespace AxiomRPG.Data.Definitions;

public record StructureDefinition(
    string Id,
    string Name,
    string Description,
    string Type,          // house, shop, inn, temple, dungeon, cave
    int Width,
    int Height,
    List<string> FloorplanLayers,
    Dictionary<string, string> Furniture,
    List<string> PossibleNPCs,
    List<string> PossibleLoot,
    Dictionary<string, string> Properties
);
