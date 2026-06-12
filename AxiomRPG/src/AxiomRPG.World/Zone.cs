namespace AxiomRPG.World;

public class Zone
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Type { get; init; } // forest, mountain, plains, urban, dungeon, etc.
    public int OffsetX { get; init; }
    public int OffsetY { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public HashSet<string> ChunkKeys { get; } = new();
    public Dictionary<string, string> Properties { get; init; } = new();

    public Zone(string id, string name, string type, int offsetX, int offsetY, int width, int height)
    {
        Id = id;
        Name = name;
        Type = type;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Width = width;
        Height = height;
    }
}
