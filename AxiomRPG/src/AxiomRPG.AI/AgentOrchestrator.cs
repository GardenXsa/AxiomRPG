using AxiomRPG.AI.Agents;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.Core.Types;
using AxiomRPG.ToolAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.AI;

public class AgentOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AgentOrchestrator> _logger;

    private WorldBuilderAgent? _worldBuilder;
    private GameMasterAgent? _gameMaster;
    private DialogAgent? _dialogAgent;
    private QuestAgent? _questAgent;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.MainMenu;
    public WorldBuilderAgent? WorldBuilder => _worldBuilder;
    public GameMasterAgent? GameMaster => _gameMaster;

    public AgentOrchestrator(
        IServiceProvider serviceProvider,
        IEventBus eventBus,
        ILogger<AgentOrchestrator> logger
    )
    {
        _serviceProvider = serviceProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public WorldBuilderAgent CreateWorldBuilder()
    {
        _worldBuilder = ActivatorUtilities.CreateInstance<WorldBuilderAgent>(_serviceProvider, "world_builder_01");
        CurrentPhase = GamePhase.WorldGeneration;
        return _worldBuilder;
    }

    public GameMasterAgent CreateGameMaster()
    {
        _gameMaster = ActivatorUtilities.CreateInstance<GameMasterAgent>(_serviceProvider, "game_master_01");
        CurrentPhase = GamePhase.Gameplay;
        return _gameMaster;
    }

    public DialogAgent CreateDialogAgent()
    {
        _dialogAgent = ActivatorUtilities.CreateInstance<DialogAgent>(_serviceProvider, "dialog_01");
        return _dialogAgent;
    }

    public QuestAgent CreateQuestAgent()
    {
        _questAgent = ActivatorUtilities.CreateInstance<QuestAgent>(_serviceProvider, "quest_01");
        return _questAgent;
    }

    public void SetPhase(GamePhase phase)
    {
        _logger.LogInformation("Phase changed: {Old} -> {New}", CurrentPhase, phase);
        CurrentPhase = phase;
    }
}
