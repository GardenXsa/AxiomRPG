using AxiomRPG.Core.Events;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.Core.Types;
using AxiomRPG.Data;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.World;

public class WorldManager
{
    private readonly DataService _dataService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<WorldManager> _logger;
    private readonly TileMap _tileMap;
    private Planet? _currentPlanet;
    private readonly Dictionary<string, Chunk> _loadedChunks = new();
    private const int LoadRadius = 3; // chunks around player to keep loaded

    public Planet? CurrentPlanet => _currentPlanet;
    public TileMap TileMap => _tileMap;

    public WorldManager(DataService dataService, IEventBus eventBus, ILogger<WorldManager> logger)
    {
        _dataService = dataService;
        _eventBus = eventBus;
        _logger = logger;
        _tileMap = new TileMap();
        _tileMap.RegisterDefaultTiles();
    }

    public async Task LoadWorldAsync(string planetId)
    {
        var metaJson = await _dataService.WorldStore.LoadWorldMetaAsync(planetId);
        if (metaJson == null)
        {
            _logger.LogError("World meta not found for planet {PlanetId}", planetId);
            return;
        }

        var meta = System.Text.Json.JsonSerializer.Deserialize<WorldMeta>(metaJson);
        if (meta == null) return;

        _currentPlanet = new Planet(meta.Id, meta.Name, meta.Description, meta.Width, meta.Height);
        _logger.LogInformation("World loaded: {Name} ({Width}x{Height} chunks)", meta.Name, meta.Width, meta.Height);

        await _eventBus.PublishAsync(new WorldCreatedEvent(planetId, meta.Name));
    }

    public async Task<Chunk> GetOrLoadChunkAsync(int chunkX, int chunkY)
    {
        var key = Chunk.MakeKey(chunkX, chunkY);
        if (_loadedChunks.TryGetValue(key, out var chunk))
            return chunk;

        // Try loading from disk
        if (_currentPlanet != null)
        {
            var chunkJson = await _dataService.WorldStore.LoadChunkAsync(_currentPlanet.Id, chunkX, chunkY);
            if (chunkJson != null)
            {
                chunk = DeserializeChunk(chunkX, chunkY, chunkJson);
                _loadedChunks[key] = chunk;
                chunk.IsLoaded = true;
                await _eventBus.PublishAsync(new ChunkLoadedEvent(_currentPlanet.Id, chunkX, chunkY));
                return chunk;
            }
        }

        // Create empty chunk
        chunk = new Chunk(chunkX, chunkY, "", "");
        _loadedChunks[key] = chunk;
        chunk.IsLoaded = true;
        return chunk;
    }

    public async Task UpdateLoadedChunksAsync(int playerChunkX, int playerChunkY)
    {
        // Load chunks within radius
        for (var dx = -LoadRadius; dx <= LoadRadius; dx++)
        for (var dy = -LoadRadius; dy <= LoadRadius; dy++)
        {
            var cx = playerChunkX + dx;
            var cy = playerChunkY + dy;
            var key = Chunk.MakeKey(cx, cy);
            if (!_loadedChunks.ContainsKey(key))
            {
                await GetOrLoadChunkAsync(cx, cy);
            }
        }

        // Unload chunks outside radius
        var keysToRemove = _loadedChunks
            .Where(kvp =>
            {
                var dx = kvp.Value.X - playerChunkX;
                var dy = kvp.Value.Y - playerChunkY;
                return Math.Abs(dx) > LoadRadius + 1 || Math.Abs(dy) > LoadRadius + 1;
            })
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            if (_loadedChunks.TryGetValue(key, out var chunk) && chunk.IsModified && _currentPlanet != null)
            {
                var json = SerializeChunk(chunk);
                await _dataService.WorldStore.SaveChunkAsync(_currentPlanet.Id, chunk.X, chunk.Y, json);
            }
            _loadedChunks.Remove(key);
        }
    }

    public Chunk? GetLoadedChunk(int chunkX, int chunkY)
    {
        var key = Chunk.MakeKey(chunkX, chunkY);
        return _loadedChunks.GetValueOrDefault(key);
    }

    public void SetPlanet(Planet planet) => _currentPlanet = planet;

    private static Chunk DeserializeChunk(int x, int y, string json)
    {
        var data = System.Text.Json.JsonSerializer.Deserialize<ChunkData>(json);
        var chunk = new Chunk(x, y, data?.RegionId ?? "", data?.BiomeId ?? "");
        if (data?.Tiles != null)
        {
            for (var ty = 0; ty < Chunk.ChunkSize && ty < data.Tiles.GetLength(0); ty++)
            for (var tx = 0; tx < Chunk.ChunkSize && tx < data.Tiles.GetLength(1); tx++)
            {
                var td = data.Tiles[ty, tx];
                chunk.SetTile(tx, ty, new AsciiTile(td.Char, td.Fg, td.Bg, td.Blocking, td.Transparent));
            }
        }
        return chunk;
    }

    private static string SerializeChunk(Chunk chunk)
    {
        var tiles = new TileData[Chunk.ChunkSize, Chunk.ChunkSize];
        for (var y = 0; y < Chunk.ChunkSize; y++)
        for (var x = 0; x < Chunk.ChunkSize; x++)
        {
            var t = chunk.GetTile(x, y);
            tiles[y, x] = new TileData(t.Character, t.ForegroundColor, t.BackgroundColor, t.IsBlocking, t.IsTransparent);
        }
        return System.Text.Json.JsonSerializer.Serialize(new ChunkData(chunk.RegionId, chunk.BiomeId, tiles));
    }

    private sealed record WorldMeta(string Id, string Name, string Description, int Width, int Height);
    private sealed record ChunkData(string RegionId, string BiomeId, TileData[,]? Tiles);
    private sealed record TileData(char Char, string Fg, string Bg, bool Blocking, bool Transparent);
}
