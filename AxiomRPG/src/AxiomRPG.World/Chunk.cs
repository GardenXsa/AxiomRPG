using AxiomRPG.Core.Types;

namespace AxiomRPG.World;

public class Chunk
{
    public const int ChunkSize = 32;

    public int X { get; }
    public int Y { get; }
    public string RegionId { get; set; }
    public string BiomeId { get; set; }
    public bool IsLoaded { get; set; }
    public bool IsModified { get; set; }

    private readonly AsciiTile[,] _tiles = new AsciiTile[ChunkSize, ChunkSize];
    private readonly List<string> _entityIds = new();

    public Chunk(int x, int y, string regionId, string biomeId)
    {
        X = x;
        Y = y;
        RegionId = regionId;
        BiomeId = biomeId;

        // Initialize with default void tile
        for (var ty = 0; ty < ChunkSize; ty++)
        for (var tx = 0; tx < ChunkSize; tx++)
            _tiles[tx, ty] = new AsciiTile(' ', "#000000", "#000000", true, false);
    }

    public AsciiTile GetTile(int x, int y)
    {
        if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkSize)
            return new AsciiTile(' ', "#000000", "#000000", true, false);
        return _tiles[x, y];
    }

    public void SetTile(int x, int y, AsciiTile tile)
    {
        if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkSize) return;
        _tiles[x, y] = tile;
        IsModified = true;
    }

    public void AddEntity(string entityId) => _entityIds.Add(entityId);
    public void RemoveEntity(string entityId) => _entityIds.Remove(entityId);
    public IReadOnlyList<string> GetEntityIds() => _entityIds.AsReadOnly();

    public string GetChunkKey() => $"{X}_{Y}";

    public static string MakeKey(int x, int y) => $"{x}_{y}";
}
