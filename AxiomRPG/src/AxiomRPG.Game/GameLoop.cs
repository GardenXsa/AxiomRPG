using System.Text;
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

    // World generation state for display
    private readonly List<string> _aiTextLines = new();
    private readonly List<string> _toolCallLog = new();
    private int _wgRound;
    private string _wgStatus = "";
    private bool _wgDone;
    private string _wgError = "";

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
            Console.Write(_renderer.ToAnsiString());

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
            Console.Write(_renderer.ToAnsiString());

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (!_settings.HandleInput(key))
                {
                    // Apply settings to the AI client config
                    ApplySettingsToConfig();
                    _gameState.CurrentPhase = GamePhase.MainMenu;
                }
            }

            await Task.Delay(50, ct);
        }
    }

    private void ApplySettingsToConfig()
    {
        var config = _serviceProvider.GetRequiredService<AIClientConfiguration>();
        if (!string.IsNullOrEmpty(_settings!.ApiKey))
            config.ApiKey = _settings.ApiKey;
        if (!string.IsNullOrEmpty(_settings.ApiBaseUrl))
            config.ApiBaseUrl = _settings.ApiBaseUrl;
        if (!string.IsNullOrEmpty(_settings.Model))
            config.Model = _settings.Model;

        _logger.LogInformation("Settings applied: Model={Model}, BaseUrl={Url}", config.Model, config.ApiBaseUrl);
    }

    private async Task RunWorldDescriptionAsync(CancellationToken ct)
    {
        _worldDesc = new WorldDescriptionScreen();
        _currentScreen = _worldDesc;
        while (_gameState.CurrentPhase == GamePhase.WorldDescription && !ct.IsCancellationRequested)
        {
            _worldDesc.Render(_renderer.UIBuffer);
            Console.Write(_renderer.ToAnsiString());

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
        // Reset state
        _aiTextLines.Clear();
        _toolCallLog.Clear();
        _wgRound = 0;
        _wgStatus = "Initializing...";
        _wgDone = false;
        _wgError = "";

        // Draw initial screen
        RenderWorldGenScreen();
        Console.Write(_renderer.ToAnsiString());

        try
        {
            var worldBuilder = _agentOrchestrator.CreateWorldBuilder();

            await foreach (var evt in worldBuilder.BuildWorldWithEventsAsync(_gameState.WorldDescription, ct))
            {
                switch (evt.Type)
                {
                    case StreamEventType.TextDelta:
                        // Accumulate AI text and track last few lines for display
                        AppendTextDelta(evt.Content);
                        break;

                    case StreamEventType.ToolCallStart:
                        _toolCallLog.Add($"Calling: {evt.Content}");
                        if (_toolCallLog.Count > 50) _toolCallLog.RemoveAt(0);
                        _wgStatus = $"Calling tool: {evt.Content}";
                        break;

                    case StreamEventType.ToolCallResult:
                        var status = evt.Detail ?? "";
                        var icon = status == "OK" ? "[OK]" : status == "ERROR" ? "[ERR]" : status == "FAILED" ? "[FAIL]" : "";
                        _toolCallLog.Add($"  {icon} {evt.Content}");
                        if (_toolCallLog.Count > 50) _toolCallLog.RemoveAt(0);
                        _wgStatus = evt.Content;
                        break;

                    case StreamEventType.RoundComplete:
                        _wgRound++;
                        _toolCallLog.Add($"--- Round {_wgRound} complete ---");
                        if (_toolCallLog.Count > 50) _toolCallLog.RemoveAt(0);
                        _wgStatus = $"Round {_wgRound} complete, continuing...";
                        break;

                    case StreamEventType.Error:
                        _wgError = evt.Content;
                        _wgStatus = $"Error: {evt.Content}";
                        break;
                }

                // Render updated screen
                RenderWorldGenScreen();
                Console.Write(_renderer.ToAnsiString());
            }

            _wgDone = true;
            _wgStatus = "World generation complete!";
            _gameState.WorldGenerationComplete = true;
            _gameState.AddMessage("World generation complete!");

            // Show completion screen for a moment
            RenderWorldGenScreen();
            Console.Write(_renderer.ToAnsiString());

            _logger.LogInformation("World generation completed in {Rounds} rounds", _wgRound);

            // Wait for user to press Enter to proceed
            _wgStatus = "Press ENTER to continue to character creation...";
            RenderWorldGenScreen();
            Console.Write(_renderer.ToAnsiString());

            while (!ct.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        _gameState.CurrentPhase = GamePhase.CharacterCreation;
                        break;
                    }
                }
                await Task.Delay(50, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "World generation failed");
            _wgError = ex.Message;
            _wgStatus = $"FAILED: {ex.Message}";

            // Show error screen
            RenderWorldGenScreen();
            Console.Write(_renderer.ToAnsiString());

            // Wait for user to press Enter to go back
            _wgStatus = "Press ENTER to return to main menu...";
            RenderWorldGenScreen();
            Console.Write(_renderer.ToAnsiString());

            while (!ct.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        _gameState.CurrentPhase = GamePhase.MainMenu;
                        break;
                    }
                }
                await Task.Delay(50, ct);
            }
        }
    }

    private void AppendTextDelta(string delta)
    {
        // Split delta by newlines and append to lines
        var parts = delta.Split('\n');
        for (var i = 0; i < parts.Length; i++)
        {
            if (i == 0)
            {
                // Append to last line
                if (_aiTextLines.Count == 0)
                    _aiTextLines.Add(parts[0]);
                else
                    _aiTextLines[^1] += parts[0];
            }
            else
            {
                _aiTextLines.Add(parts[i]);
            }
        }

        // Keep only last 30 lines for display
        while (_aiTextLines.Count > 30)
            _aiTextLines.RemoveAt(0);
    }

    private void RenderWorldGenScreen()
    {
        var buf = _renderer.UIBuffer;
        buf.Clear();

        var w = buf.Width;

        // Header
        buf.WriteCentered(1, "═══ AI World Builder ═══", "#FFD700");
        buf.WriteCentered(2, _gameState.WorldDescription.Length > w - 8
            ? _gameState.WorldDescription[..(w - 8)] + "..."
            : _gameState.WorldDescription, "#888888");

        // Status line
        var statusColor = _wgDone ? "#44FF44" : !string.IsNullOrEmpty(_wgError) ? "#FF4444" : "#FFD700";
        var statusText = _wgStatus;
        if (statusText.Length > w - 6) statusText = statusText[..(w - 6)];
        buf.Write(3, 4, statusText, statusColor);

        // Separator
        buf.Write(2, 5, new string('═', w - 4), "#444444");

        // Tool call log (left side)
        buf.Write(2, 6, "Tool Calls:", "#FFD700");
        var logStartY = 7;
        var maxLogLines = Math.Min(_toolCallLog.Count, 15);
        for (var i = 0; i < maxLogLines; i++)
        {
            var lineIdx = _toolCallLog.Count - maxLogLines + i;
            var line = _toolCallLog[lineIdx];
            var lineColor = line.Contains("[ERR]") || line.Contains("[FAIL]") ? "#FF6666" :
                            line.Contains("[OK]") ? "#66FF66" :
                            line.Contains("---") ? "#666666" :
                            line.Contains("Calling:") ? "#FFD700" : "#AAAAAA";

            var truncated = line.Length > w / 2 - 4 ? line[..(w / 2 - 4)] : line;
            buf.Write(2, logStartY + i, truncated, lineColor);
        }

        // AI text output (right side)
        var textX = w / 2 + 2;
        buf.Write(textX, 6, "AI Output:", "#FFD700");
        var maxTextLines = Math.Min(_aiTextLines.Count, 15);
        for (var i = 0; i < maxTextLines; i++)
        {
            var lineIdx = _aiTextLines.Count - maxTextLines + i;
            var line = _aiTextLines[lineIdx];
            var truncated = line.Length > w / 2 - 4 ? line[..(w / 2 - 4)] : line;
            buf.Write(textX, logStartY + i, truncated, "#C0C0C0");
        }

        // Bottom separator
        var bottomY = 23;
        buf.Write(2, bottomY, new string('═', w - 4), "#444444");

        // Progress info
        buf.Write(2, bottomY + 1, $"Round: {_wgRound}  |  Tool calls: {_toolCallLog.Count(x => x.Contains("Calling:"))}  |  Status: {(_wgDone ? "COMPLETE" : !string.IsNullOrEmpty(_wgError) ? "ERROR" : "WORKING")}", "#888888");

        // Footer
        buf.WriteCentered(buf.Height - 2,
            _wgDone ? "World built! Press ENTER to continue" :
            !string.IsNullOrEmpty(_wgError) ? "Generation failed. Press ENTER to go back" :
            "AI is building your world... please wait", "#666666");
    }

    private async Task RunCharacterCreationAsync(CancellationToken ct)
    {
        _charCreation = new CharacterCreationScreen();
        _charCreation.SetAvailableStats(new List<string> { "STR", "DEX", "CON", "INT", "WIS", "CHA" }, 10f);
        _currentScreen = _charCreation;

        while (_gameState.CurrentPhase == GamePhase.CharacterCreation && !ct.IsCancellationRequested)
        {
            _charCreation.Render(_renderer.UIBuffer);
            Console.Write(_renderer.ToAnsiString());

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (!_charCreation.HandleInput(key))
                {
                    // Character created — transition to gameplay
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

        // Initialize Game Master for the player
        var gameMaster = _agentOrchestrator.CreateGameMaster();
        await gameMaster.StartAsync(ct);
        var gmReady = false;
        var gmText = new StringBuilder();
        var gmStreaming = false;

        // Start GM initialization in background
        var gmInitTask = Task.Run(async () =>
        {
            try
            {
                var charDesc = _charCreation?.CharacterDescription ?? "A brave adventurer";
                var playerId = _gameState.CurrentPlayerId ?? "player_01";
                await foreach (var evt in gameMaster.StreamWithEventsAsync(
                    $"A new player has entered the world. Here is their character description:\n\nPlayer ID: {playerId}\nDescription: {charDesc}\n\nPlease:\n1. READ the region data to find a suitable spawn point\n2. SPAWN the player at an appropriate location\n3. GIVE them starting items appropriate to their character\n4. CREATE an initial quest for them\n5. SPAWN any nearby creatures that make sense for the location", ct))
                {
                    if (evt.Type == StreamEventType.TextDelta)
                    {
                        lock (gmText)
                        {
                            gmText.Append(evt.Content);
                            gmStreaming = true;
                        }
                    }
                    else if (evt.Type == StreamEventType.ToolCallResult)
                    {
                        _gameState.AddMessage($"GM: {evt.Content}");
                    }
                }
                gmReady = true;
                gmStreaming = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GM initialization failed");
                lock (gmText)
                {
                    gmText.Append($"\n[GM Error: {ex.Message}]");
                }
                gmReady = true;
                gmStreaming = false;
            }
        }, ct);

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
                await HandleGameplayInputAsync(key, gameMaster, ct);
            }

            // Update simulation
            _simulation.Update(deltaTime);

            // Render world
            _renderer.RenderWorld();

            // Get player info for UI
            var playerName = _charCreation?.CharacterName ?? "Hero";
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

            // Show GM text if streaming
            if (gmStreaming || gmText.Length > 0)
            {
                var gmDisplay = gmText.ToString();
                var gmLines = gmDisplay.Split('\n').TakeLast(3).ToList();
                for (var i = 0; i < gmLines.Count; i++)
                {
                    var line = gmLines[i];
                    if (line.Length > _renderer.UIBuffer.Width - 4)
                        line = line[..(_renderer.UIBuffer.Width - 4)];
                    _renderer.UIBuffer.Write(2, _renderer.UIBuffer.Height - 8 + i,
                        gmStreaming ? line + "▌" : line, "#FFD700");
                }
            }

            Console.Write(_renderer.ToAnsiString());

            await Task.Delay(33, ct); // ~30 FPS
        }

        _simulation.Stop();
    }

    private async Task HandleGameplayInputAsync(ConsoleKeyInfo key, GameMasterAgent gameMaster, CancellationToken ct)
    {
        if (_gameState.CurrentPlayerId == null) return;

        var player = _entityManager.GetEntity(EntityId.From(_gameState.CurrentPlayerId));
        if (player == null) return;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow or ConsoleKey.W:
            {
                var moveSystem = _serviceProvider.GetRequiredService<MovementSystem>();
                await moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, 0, -1);
                break;
            }
            case ConsoleKey.DownArrow or ConsoleKey.S:
            {
                var moveSystem = _serviceProvider.GetRequiredService<MovementSystem>();
                await moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, 0, 1);
                break;
            }
            case ConsoleKey.LeftArrow or ConsoleKey.A:
            {
                var moveSystem = _serviceProvider.GetRequiredService<MovementSystem>();
                await moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, -1, 0);
                break;
            }
            case ConsoleKey.RightArrow or ConsoleKey.D:
            {
                var moveSystem = _serviceProvider.GetRequiredService<MovementSystem>();
                await moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, 1, 0);
                break;
            }
            case ConsoleKey.Escape:
                _gameState.CurrentPhase = GamePhase.MainMenu;
                break;
        }

        // Update camera to follow player
        var pos = _entityManager.GetComponent<PositionComponent>(player.Id);
        if (pos != null)
        {
            _renderer.Camera.CenterOn(pos.X, pos.Y);
        }
    }
}
