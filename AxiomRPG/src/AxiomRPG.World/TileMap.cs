using AxiomRPG.Core.Types;

namespace AxiomRPG.World;

public class TileMap
{
    private readonly Dictionary<string, AsciiTile> _tileDefinitions = new();

    public void RegisterTile(string id, AsciiTile tile) => _tileDefinitions[id] = tile;

    public AsciiTile? GetTile(string id) => _tileDefinitions.GetValueOrDefault(id);

    public AsciiTile GetTileOrFallback(string id, AsciiTile fallback) =>
        _tileDefinitions.GetValueOrDefault(id, fallback);

    public void RegisterDefaultTiles()
    {
        RegisterTile("void", new AsciiTile(' ', "#000000", "#000000", true, false));
        RegisterTile("grass", new AsciiTile('.', "#228B22", "#1a1a1a", false, true));
        RegisterTile("wall", new AsciiTile('#', "#808080", "#404040", true, false));
        RegisterTile("floor", new AsciiTile('.', "#C0C0C0", "#2a2a2a", false, true));
        RegisterTile("water", new AsciiTile('~', "#4169E1", "#1a1a4a", true, false));
        RegisterTile("tree", new AsciiTile('T', "#006400", "#1a1a1a", true, false));
        RegisterTile("sand", new AsciiTile('.', "#F4A460", "#3a3020", false, true));
        RegisterTile("mountain", new AsciiTile('^', "#A0A0A0", "#404040", true, false));
        RegisterTile("snow", new AsciiTile('.', "#FFFFFF", "#808080", false, true));
        RegisterTile("road", new AsciiTile('=', "#8B4513", "#2a2a1a", false, true));
        RegisterTile("door_closed", new AsciiTile('+', "#8B4513", "#2a2a1a", true, false));
        RegisterTile("door_open", new AsciiTile('/', "#8B4513", "#2a2a1a", false, true));
        RegisterTile("stairs_down", new AsciiTile('>', "#FFD700", "#2a2a2a", false, true));
        RegisterTile("stairs_up", new AsciiTile('<', "#FFD700", "#2a2a2a", false, true));
    }
}
