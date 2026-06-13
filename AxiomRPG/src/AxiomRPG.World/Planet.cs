using AxiomRPG.Core.Types;

namespace AxiomRPG.World;

public class Planet
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public int Width { get; init; }  // in chunks
    public int Height { get; init; } // in chunks
    public Dictionary<string, Continent> Continents { get; } = new();
    public Dictionary<string, string> Properties { get; init; } = new();

    public Planet(string id, string name, string description, int width = 1024, int height = 1024)
    {
        Id = id;
        Name = name;
        Description = description;
        Width = width;
        Height = height;
    }
}
