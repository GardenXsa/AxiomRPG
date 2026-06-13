namespace AxiomRPG.Data.Definitions;

public record BiomeDefinition(
    string Id,
    string Name,
    string Description,
    string DefaultTile,
    Dictionary<string, float> TileWeights,
    List<string> Flora,
    List<string> Fauna,
    string WeatherRules,
    float TemperatureMin,
    float TemperatureMax,
    float Rainfall,
    List<string> AllowedStructures,
    Dictionary<string, string> Properties
);
