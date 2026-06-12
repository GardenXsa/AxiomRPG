using System.Text.Json;
using AxiomRPG.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.Data;

public class DefinitionRepository<T> : IRepository<T> where T : class
{
    private static readonly JsonSerializerOptions WriteIndentedOptions = new() { WriteIndented = true };
    private readonly IDataStore _store;
    private readonly string _directory;
    private readonly ILogger<DefinitionRepository<T>> _logger;
    private readonly Dictionary<string, T> _cache = new();
    private bool _loaded;

    public DefinitionRepository(IDataStore store, string directory, ILogger<DefinitionRepository<T>> logger)
    {
        _store = store;
        _directory = directory;
        _logger = logger;
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        await EnsureLoadedAsync();
        return _cache.GetValueOrDefault(id);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        await EnsureLoadedAsync();
        return _cache.Values.ToList();
    }

    public async Task<IReadOnlyList<T>> QueryAsync(Func<T, bool> predicate)
    {
        await EnsureLoadedAsync();
        return _cache.Values.Where(predicate).ToList();
    }

    public async Task SaveAsync(T entity)
    {
        var idProp = entity.GetType().GetProperty("Id");
        var id = idProp?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
        var json = JsonSerializer.Serialize(entity, WriteIndentedOptions);
        await _store.WriteTextAsync($"{_directory}/{id}.json", json);
        _cache[id] = entity;
    }

    public async Task DeleteAsync(string id)
    {
        await _store.DeleteAsync($"{_directory}/{id}.json");
        _cache.Remove(id);
    }

    public async Task<bool> ExistsAsync(string id)
    {
        await EnsureLoadedAsync();
        return _cache.ContainsKey(id);
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;

        var files = await _store.ListFilesAsync(_directory, "*.json");
        foreach (var file in files)
        {
            try
            {
                var content = await _store.ReadTextAsync(file);
                if (content == null) continue;
                var definition = JsonSerializer.Deserialize<T>(content);
                if (definition == null) continue;

                var idProp = typeof(T).GetProperty("Id");
                var id = idProp?.GetValue(definition)?.ToString();
                if (id != null) _cache[id] = definition;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load definition from {File}", file);
            }
        }
        _logger.LogInformation("Loaded {Count} definitions from {Dir}", _cache.Count, _directory);
    }
}
