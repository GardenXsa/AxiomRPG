using AxiomRPG.AI;
using AxiomRPG.AI.Agents;
using AxiomRPG.Components;
using AxiomRPG.Core.Types;
using AxiomRPG.ECS;
using AxiomRPG.Rendering;
using AxiomRPG.Rendering.Screens;
using AxiomRPG.Simulation;
using AxiomRPG.Simulation.Systems;
using AxiomRPG.ToolAPI;
using AxiomRPG.World;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.Game;

public class GameLoop
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GameState _gameState;
    private readonly SimulationEngine _simulation;
    private readonly ASCIIRenderer _renderer;
    private readonly EntityManager _entityManager;
    private readonly WorldManager _worldManager;
    private readonly AgentOrchestrator _agentOrchestrator;
    private readonly ToolDispatcher _toolDispatcher;
    private readonly ILogger<GameLoop> _logger;

    private MainMenuScreen? _mainMenu;
    private SettingsScreen? _settings;
    private WorldDescriptionScreen? _worldDesc;
    private CharacterCreationScreen? _charCreation;
    private IScreen? _currentScreen;

    public GameLoop(
        IServiceProvider serviceProvider,
        GameState gameState,
        SimulationEngine simulation,
        ASCIIRenderer renderer,
        EntityManager entityManager,
        WorldManager worldManager,
        AgentOrchestrator agentOrchestrator,
        ToolDispatcher toolDispatcher,
        ILogger<GameLoop> logger
    )
    {
        _serviceProvider = serviceProvider;
        _gameState = gameState;
        _simulation = simulation;
        _renderer = renderer;
        _entityManager = entityManager;
        _worldManager = worldManager;
        _agentOrchestrator = agentOrchestrator;
        _toolDispatcher = toolDispatcher;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("AxiomRPG starting...");
        
        // Register simulation systems
        foreach (var system in _serviceProvider.GetServices<ISystem>())
        {
            _simulation.AddSystem(system);
        }

        _mainMenu = new MainMenuScreen();
        _settings = new SettingsScreen();
        _worldDesc = new WorldDescriptionScreen();
        _charCreation = new CharacterCreationScreen();
        
        _currentScreen = _mainMenu;
        _gameState.CurrentPhase = GamePhase.MainMenu;

        while (_gameState.IsRunning && !ct.IsCancellationRequested)
        {
            switch (_gameState.CurrentPhase)
            {
                case GamePhase.MainMenu:
                    await RunMainMenuAsync(ct);
                    break;
                case GamePhase.Settings:
                    await RunSettingsAsync(ct);
                    break;
                case GamePhase.WorldDescription:
                    await RunWorldDescriptionAsync(ct);
                    break;
                case GamePhase.WorldGeneration:
                    await RunWorldGenerationAsync(ct);
                    break;
                case GamePhase.CharacterCreation:
                    await RunCharacterCreationAsync(ct);
                    break;
                case GamePhase.Gameplay:
                    await RunGameplayAsync(ct);
                    break;
                case GamePhase.GameOver:
                    _gameState.IsRunning = false;
                    break;
            }
        }
        
        _logger.LogInformation("AxiomRPG shutting down.");
    }

    private async Task RunMainMenuAsync(CancellationToken ct)
    {
        _currentScreen = _mainMenu;
        while (_gameState.CurrentPhase == GamePhase.MainMenu && !ct.IsCancellationRequested)
        {
            _mainMenu!.Render(_renderer.UIBuffer);
            Console.Write(_renderer.ToPlainText());
            
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (!_mainMenu.HandleInput(key))
                {
                    _gameState.CurrentPhase = _mainMenu.GetSelectedPhase();
                    _gameState.AddMessage($"Menu selection: {_gameState.CurrentPhase}");
                }
            }
            
            await Task.Delay(50, ct);
        }
    }

    private async Task RunSettingsAsync(CancellationToken ct)
    {
        _currentScreen = _settings;
        while (_gameState.CurrentPhase == GamePhase.Settings && !ct.IsCancellationRequested)
        {
            _settings!.Render(_renderer.UIBuffer);
            Console.Write(_renderer.ToPlainText());
            
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (!_settings.HandleInput(key))
                {
                    _gameState.CurrentPhase = GamePhase.MainMenu;
                }
            }
            
            await Task.Delay(50, ct);
        }
    }

    private async Task RunWorldDescriptionAsync(CancellationToken ct)
    {
        _worldDesc = new WorldDescriptionScreen();
        _currentScreen = _worldDesc;
        while (_gameState.CurrentPhase == GamePhase.WorldDescription && !ct.IsCancellationRequested)
        {
            _worldDesc.Render(_renderer.UIBuffer);
            Console.Write(_renderer.ToPlainText());
            
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (!_worldDesc.HandleInput(key))
                {
                    if (!string.IsNullOrWhiteSpace(_worldDesc.WorldDescription))
                    {
                        _gameState.WorldDescription = _worldDesc.WorldDescription;
                        _gameState.CurrentPhase = GamePhase.WorldGeneration;
                    }
                    else
                    {
                        _gameState.CurrentPhase = GamePhase.MainMenu;
                    }
                }
            }
            
            await Task.Delay(50, ct);
        }
    }

    private async Task RunWorldGenerationAsync(CancellationToken ct)
    {
        _renderer.UIBuffer.Clear();
        _renderer.UIBuffer.WriteCentered(10, "═══ AI is building your world... ═══", "#FFD700");
        _renderer.UIBuffer.WriteCentered(12, _gameState.WorldDescription[..Math.Min(_gameState.WorldDescription.Length, 60)], "#AAAAAA");
        _renderer.UIBuffer.WriteCentered(20, "Please wait, this may take a minute...", "#666666");
        Console.Write(_renderer.ToPlainText());

        try
        {
            var worldBuilder = _agentOrchestrator.CreateWorldBuilder();
            
            var outputBuilder = new System.Text.StringBuilder();
            await foreach (var chunk in worldBuilder.BuildWorldAsync(_gameState.WorldDescription, ct))
            {
                outputBuilder.Append(chunk);
                // Show streaming progress
                var output = outputBuilder.ToString();
                var lastLines = output.Split('\n').TakeLast(5);
                _renderer.UIBuffer.Clear();
                _renderer.UIBuffer.WriteCentered(5, "═══ AI World Builder ═══", "#FFD700");
                _renderer.UIBuffer.WriteCentered(7, "Streaming...", "#AAAAAA");
                for (var i = 0; i < lastLines.Count(); i++)
                {
                    var line = lastLines.ElementAt(i)[..Math.Min(lastLines.ElementAt(i).Length, 76)];
                    _renderer.UIBuffer.WriteCentered(10 + i, line, "#C0C0C0");
                }
                Console.Write(_renderer.ToPlainText());
            }
            
            _gameState.WorldGenerationComplete = true;
            _gameState.CurrentPhase = GamePhase.CharacterCreation;
            _gameState.AddMessage("World generation complete!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "World generation failed");
            _gameState.AddMessage($"World generation error: {ex.Message}");
            _gameState.CurrentPhase = GamePhase.MainMenu;
        }
    }

    private async Task RunCharacterCreationAsync(CancellationToken ct)
    {
        _charCreation = new CharacterCreationScreen();
        // Set default stats if stat system is loaded
        _charCreation.SetAvailableStats(new List<string> { "STR", "DEX", "CON", "INT", "WIS", "CHA" }, 10f);
        _currentScreen = _charCreation;
        
        while (_gameState.CurrentPhase == GamePhase.CharacterCreation && !ct.IsCancellationRequested)
        {
            _charCreation.Render(_renderer.UIBuffer);
            Console.Write(_renderer.ToPlainText());
            
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (!_charCreation.HandleInput(key))
                {
                    // Character created — hand off to Game Master
                    _gameState.CurrentPhase = GamePhase.Gameplay;
                    
                    // Create player entity
                    var player = _entityManager.CreateEntity(EntityId.From("player_01"));
                    _entityManager.AddComponent(player.Id, new CreatureTypeComponent
                    {
                        Category = "humanoid",
                        Race = "custom",
                        Size = "medium"
                    });
                    _entityManager.AddComponent(player.Id, new RenderableComponent
                    {
                        Tile = new AsciiTile('@', "#FFD700", "#000000"),
                        RenderLayer = 3,
                        IsVisible = true
                    });
                    _entityManager.AddComponent(player.Id, new HealthComponent
                    {
                        MaxHp = 100,
                        CurrentHp = 100,
                        MaxStamina = 50,
                        CurrentStamina = 50
                    });
                    
                    var stats = new StatsComponent
                    {
                        BaseValues = _charCreation.AllocatedStats.ToDictionary(k => k.Key.ToLower(), k => k.Value),
                        CurrentValues = _charCreation.AllocatedStats.ToDictionary(k => k.Key.ToLower(), k => k.Value)
                    };
                    _entityManager.AddComponent(player.Id, stats);
                    
                    _gameState.CurrentPlayerId = player.Id.Value;
                    _gameState.AddMessage($"Character created: {_charCreation.CharacterName}");
                }
            }
            
            await Task.Delay(50, ct);
        }
    }

    private async Task RunGameplayAsync(CancellationToken ct)
    {
        _simulation.Start();
        _gameState.AddMessage("You find yourself in a new world...");
        
        var lastRender = DateTime.UtcNow;
        
        while (_gameState.CurrentPhase == GamePhase.Gameplay && _gameState.IsRunning && !ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var deltaTime = (float)(now - lastRender).TotalSeconds;
            lastRender = now;

            // Process input
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                await HandleGameplayInputAsync(key);
            }

            // Update simulation
            _simulation.Update(deltaTime);
            
            // Render
            _renderer.RenderWorld();
            
            // Get player info for UI
            var playerName = "Hero";
            float hp = 100, maxHp = 100, stamina = 50, maxStamina = 50;
            var location = "Unknown";
            
            if (_gameState.CurrentPlayerId != null)
            {
                var player = _entityManager.GetEntity(EntityId.From(_gameState.CurrentPlayerId));
                if (player != null)
                {
                    var health = _entityManager.GetComponent<HealthComponent>(player.Id);
                    if (health != null) { hp = health.CurrentHp; maxHp = health.MaxHp; stamina = health.CurrentStamina; maxStamina = health.MaxStamina; }
                }
            }
            
            _renderer.RenderUI(playerName, hp, maxHp, stamina, maxStamina, location, 
                _simulation.GameTime.ToString(), _gameState.MessageLog);
            
            Console.Write(_renderer.ToPlainText());
            
            await Task.Delay(33, ct); // ~30 FPS
        }
        
        _simulation.Stop();
    }

    private async Task HandleGameplayInputAsync(ConsoleKeyInfo key)
    {
        if (_gameState.CurrentPlayerId == null) return;
        
        var player = _entityManager.GetEntity(EntityId.From(_gameState.CurrentPlayerId));
        if (player == null) return;
        
        var pos = _entityManager.GetComponent<PositionComponent>(player.Id);
        if (pos == null) return;

        var moveSystem = _serviceProvider.GetRequiredService<MovementSystem>();
        
        switch (key.Key)
        {
            case ConsoleKey.UpArrow or ConsoleKey.W:
                await moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, 0, -1);
                break;
            case ConsoleKey.DownArrow or ConsoleKey.S:
                await moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, 0, 1);
                break;
            case ConsoleKey.LeftArrow or ConsoleKey.A:
                await moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, -1, 0);
                break;
            case ConsoleKey.RightArrow or ConsoleKey.D:
                await moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, 1, 0);
                break;
            case ConsoleKey.Escape:
                _gameState.CurrentPhase = GamePhase.MainMenu;
                break;
        }
        
        // Update camera to follow player
        var newPos = _entityManager.GetComponent<PositionComponent>(player.Id);
        if (newPos != null)
        {
            _renderer.Camera.CenterOn(newPos.X, newPos.Y);
        }
    }
}
