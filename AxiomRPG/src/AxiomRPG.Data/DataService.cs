using AxiomRPG.Data.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.Data;

public class DataService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataStore _store;
    private readonly ILogger<DataService> _logger;

    // Lazy-loaded repositories
    private DefinitionRepository<BiomeDefinition>? _biomes;
    private DefinitionRepository<CreatureTemplateDefinition>? _creatures;
    private DefinitionRepository<ItemDefinition>? _items;
    private DefinitionRepository<RaceDefinition>? _races;
    private DefinitionRepository<StructureDefinition>? _structures;
    private DefinitionRepository<FactionDefinition>? _factions;
    private DefinitionRepository<StatSystemDefinition>? _statSystems;
    private DefinitionRepository<LocationDefinition>? _locations;

    public DefinitionRepository<BiomeDefinition> Biomes => _biomes ??= new(_store, "definitions/biomes", _serviceProvider.GetRequiredService<ILogger<DefinitionRepository<BiomeDefinition>>>());
    public DefinitionRepository<CreatureTemplateDefinition> Creatures => _creatures ??= new(_store, "definitions/creatures", _serviceProvider.GetRequiredService<ILogger<DefinitionRepository<CreatureTemplateDefinition>>>());
    public DefinitionRepository<ItemDefinition> Items => _items ??= new(_store, "definitions/items", _serviceProvider.GetRequiredService<ILogger<DefinitionRepository<ItemDefinition>>>());
    public DefinitionRepository<RaceDefinition> Races => _races ??= new(_store, "definitions/races", _serviceProvider.GetRequiredService<ILogger<DefinitionRepository<RaceDefinition>>>());
    public DefinitionRepository<StructureDefinition> Structures => _structures ??= new(_store, "definitions/structures", _serviceProvider.GetRequiredService<ILogger<DefinitionRepository<StructureDefinition>>>());
    public DefinitionRepository<FactionDefinition> Factions => _factions ??= new(_store, "definitions/factions", _serviceProvider.GetRequiredService<ILogger<DefinitionRepository<FactionDefinition>>>());
    public DefinitionRepository<StatSystemDefinition> StatSystems => _statSystems ??= new(_store, "definitions/stat_systems", _serviceProvider.GetRequiredService<ILogger<DefinitionRepository<StatSystemDefinition>>>());
    public DefinitionRepository<LocationDefinition> Locations => _locations ??= new(_store, "definitions/locations", _serviceProvider.GetRequiredService<ILogger<DefinitionRepository<LocationDefinition>>>());

    public WorldStore WorldStore { get; }
    public DataValidator Validator { get; }

    public DataService(IServiceProvider serviceProvider, IDataStore store, ILogger<DataService> logger)
    {
        _serviceProvider = serviceProvider;
        _store = store;
        _logger = logger;
        WorldStore = new WorldStore(store, serviceProvider.GetRequiredService<ILogger<WorldStore>>());
        Validator = new DataValidator();
    }
}
