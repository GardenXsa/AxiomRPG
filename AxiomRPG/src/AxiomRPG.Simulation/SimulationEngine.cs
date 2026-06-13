using AxiomRPG.Simulation.Systems;
using AxiomRPG.Core.Events;
using AxiomRPG.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.Simulation;

public class SimulationEngine
{
    private readonly List<ISystem> _systems = new();
    private readonly ILogger<SimulationEngine> _logger;
    private readonly IEventBus _eventBus;

    public GameTime GameTime { get; } = new();
    public bool IsRunning { get; private set; }
    public float TimeScale { get; set; } = 1.0f;

    public SimulationEngine(ILogger<SimulationEngine> logger, IEventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    public void AddSystem(ISystem system)
    {
        _systems.Add(system);
        _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        system.Initialize();
        _logger.LogInformation("System added: {Type} (priority {Priority})", system.GetType().Name, system.Priority);
    }

    public void Start() => IsRunning = true;
    public void Stop() => IsRunning = false;

    public void Update(float realDeltaTime)
    {
        if (!IsRunning) return;

        var dt = realDeltaTime * TimeScale;
        GameTime.AdvanceTicks((int)(dt * 100));

        foreach (var system in _systems)
        {
            system.Update(dt);
        }
    }
}
