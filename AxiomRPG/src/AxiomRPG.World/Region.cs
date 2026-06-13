namespace AxiomRPG.World;

public class Region
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string BiomeId { get; init; }
    public string ContinentId { get; init; }
    public int OffsetX { get; init; }
    public int OffsetY { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public Dictionary<string, Zone> Zones { get; } = new();
    public List<string> Locations { get; } = new(); // city, village, dungeon IDs
    public Dictionary<string, string> Properties { get; init; } = new();

    public Region(string id, string name, string description, string biomeId, string continentId, int offsetX, int offsetY, int width, int height)
    {
        Id = id;
        Name = name;
        Description = description;
        BiomeId = biomeId;
        ContinentId = continentId;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Width = width;
        Height = height;
    }
}
