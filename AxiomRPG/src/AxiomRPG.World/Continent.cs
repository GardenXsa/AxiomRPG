namespace AxiomRPG.World;

public class Continent
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public int OffsetX { get; init; } // chunk offset within planet
    public int OffsetY { get; init; }
    public int Width { get; init; }   // in chunks
    public int Height { get; init; }
    public Dictionary<string, Region> Regions { get; } = new();
    public Dictionary<string, string> Properties { get; init; } = new();

    public Continent(string id, string name, string description, int offsetX, int offsetY, int width, int height)
    {
        Id = id;
        Name = name;
        Description = description;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Width = width;
        Height = height;
    }
}
