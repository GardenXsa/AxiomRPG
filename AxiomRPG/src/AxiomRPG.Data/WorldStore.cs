using Microsoft.Extensions.Logging;

namespace AxiomRPG.Data;

public class WorldStore
{
    private readonly IDataStore _store;
    private readonly ILogger<WorldStore> _logger;

    public WorldStore(IDataStore store, ILogger<WorldStore> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task SaveChunkAsync(string planetId, int chunkX, int chunkY, string jsonData)
    {
        var path = $"world/{planetId}/chunks/{chunkX}_{chunkY}.json";
        await _store.WriteTextAsync(path, jsonData);
    }

    public async Task<string?> LoadChunkAsync(string planetId, int chunkX, int chunkY)
    {
        var path = $"world/{planetId}/chunks/{chunkX}_{chunkY}.json";
        return await _store.ReadTextAsync(path);
    }

    public async Task SaveEntityAsync(string planetId, string entityId, string jsonData)
    {
        var path = $"world/{planetId}/entities/{entityId}.json";
        await _store.WriteTextAsync(path, jsonData);
    }

    public async Task<string?> LoadEntityAsync(string planetId, string entityId)
    {
        var path = $"world/{planetId}/entities/{entityId}.json";
        return await _store.ReadTextAsync(path);
    }

    public async Task SaveWorldMetaAsync(string planetId, string jsonData)
    {
        var path = $"world/{planetId}/meta.json";
        await _store.WriteTextAsync(path, jsonData);
    }

    public async Task<string?> LoadWorldMetaAsync(string planetId)
    {
        var path = $"world/{planetId}/meta.json";
        return await _store.ReadTextAsync(path);
    }

    public async Task SaveRegionAsync(string planetId, string regionId, string jsonData)
    {
        var path = $"world/{planetId}/regions/{regionId}.json";
        await _store.WriteTextAsync(path, jsonData);
    }

    public async Task<string?> LoadRegionAsync(string planetId, string regionId)
    {
        var path = $"world/{planetId}/regions/{regionId}.json";
        return await _store.ReadTextAsync(path);
    }

    public async Task<IReadOnlyList<string>> GetAllRegionsAsync(string planetId)
    {
        return await _store.ListFilesAsync($"world/{planetId}/regions", "*.json");
    }

    public async Task<IReadOnlyList<string>> GetAllEntitiesAsync(string planetId)
    {
        return await _store.ListFilesAsync($"world/{planetId}/entities", "*.json");
    }
}
