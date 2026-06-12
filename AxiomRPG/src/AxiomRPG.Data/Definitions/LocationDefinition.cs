namespace AxiomRPG.Data.Definitions;

public record LocationDefinition(
    string Id,
    string Name,
    string Description,
    string Type,            // city, village, dungeon, cave, ruins, camp
    string BiomeId,
    List<string> Districts,
    List<string> Structures,
    List<string> Connections,
    int SizeX,
    int SizeY,
    Dictionary<string, string> Properties
);
