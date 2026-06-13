using System.Collections.Concurrent;
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

public class GameLoop : IDisposable
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

    // Event queue for background AI tasks → main thread communication
    private readonly ConcurrentQueue<StreamEvent> _eventQueue = new();
    private CancellationTokenSource _aiCts = new();

    private MainMenuScreen? _mainMenu;
    private SettingsScreen? _settings;
    private WorldDescriptionScreen? _worldDesc;
    private CharacterCreationScreen? _charCreation;

    // World generation state
    private readonly List<string> _aiTextLines = new();
    private readonly List<string> _toolCallLog = new();
    private int _wgRound;
    private string _wgStatus = "";
    private bool _wgDone;
    private string _wgError = "";
    private Task? _wgTask;

    // Gameplay state
    private GameMasterAgent? _gameMaster;
    private readonly StringBuilder _gmText = new();
    private bool _gmStreaming;
    private Task? _gmInitTask;

    // Timing
    private DateTime _lastUpdate = DateTime.UtcNow;

    public bool IsRunning => _gameState.IsRunning;

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

    public void Init()
    {
        _logger.LogInformation("AxiomRPG starting...");

        foreach (var system in _serviceProvider.GetServices<ISystem>())
        {
            _simulation.AddSystem(system);
        }

        _mainMenu = new MainMenuScreen();
        _settings = new SettingsScreen();
        _worldDesc = new WorldDescriptionScreen();
        _charCreation = new CharacterCreationScreen();

        _gameState.CurrentPhase = GamePhase.MainMenu;
    }

    public RenderBuffer GetRenderBuffer() => _renderer.UIBuffer;

    /// <summary>
    /// Called every frame from the main (Raylib) thread. Input may be null if no key pressed.
    /// </summary>
    public void Update(ConsoleKeyInfo? input)
    {
        var now = DateTime.UtcNow;
        var deltaTime = (float)(now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        // Drain event queue from background AI tasks
        while (_eventQueue.TryDequeue(out var evt))
        {
            ProcessStreamEvent(evt);
        }

        switch (_gameState.CurrentPhase)
        {
            case GamePhase.MainMenu:
                UpdateMainMenu(input);
                break;
            case GamePhase.Settings:
                UpdateSettings(input);
                break;
            case GamePhase.WorldDescription:
                UpdateWorldDescription(input);
                break;
            case GamePhase.WorldGeneration:
                UpdateWorldGeneration(input);
                break;
            case GamePhase.CharacterCreation:
                UpdateCharacterCreation(input);
                break;
            case GamePhase.Gameplay:
                UpdateGameplay(input, deltaTime);
                break;
            case GamePhase.GameOver:
                _gameState.IsRunning = false;
                break;
        }
    }

    public void Shutdown()
    {
        _aiCts.Cancel();
        _simulation.Stop();
    }

    // ─── Event processing ────────────────────────────────────────────

    private void ProcessStreamEvent(StreamEvent evt)
    {
        switch (_gameState.CurrentPhase)
        {
            case GamePhase.WorldGeneration:
                // Check for completion signal
                if (evt.Type == StreamEventType.RoundComplete && evt.Content == "WORLD_GEN_DONE")
                {
                    _wgDone = true;
                    _wgStatus = "World generation complete!";
                    _gameState.WorldGenerationComplete = true;
                    _gameState.AddMessage("World generation complete!");
                    _logger.LogInformation("World generation completed in {Rounds} rounds", _wgRound);
                }
                else
                {
                    ProcessWorldGenEvent(evt);
                }
                break;
            case GamePhase.Gameplay:
                ProcessGameplayEvent(evt);
                break;
        }
    }

    private void ProcessWorldGenEvent(StreamEvent evt)
    {
        switch (evt.Type)
        {
            case StreamEventType.TextDelta:
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
    }

    private void ProcessGameplayEvent(StreamEvent evt)
    {
        if (evt.Type == StreamEventType.TextDelta)
        {
            lock (_gmText)
            {
                _gmText.Append(evt.Content);
                _gmStreaming = true;
            }
        }
        else if (evt.Type == StreamEventType.ToolCallResult)
        {
            _gameState.AddMessage($"GM: {evt.Content}");
        }
        else if (evt.Type == StreamEventType.RoundComplete)
        {
            lock (_gmText)
            {
                _gmStreaming = false;
            }
        }
    }

    // ─── Main Menu ───────────────────────────────────────────────────

    private void UpdateMainMenu(ConsoleKeyInfo? input)
    {
        _mainMenu!.Render(_renderer.UIBuffer);

        if (input.HasValue)
        {
            if (!_mainMenu.HandleInput(input.Value))
            {
                _gameState.CurrentPhase = _mainMenu.GetSelectedPhase();
                _gameState.AddMessage($"Menu selection: {_gameState.CurrentPhase}");
            }
        }
    }

    // ─── Settings ────────────────────────────────────────────────────

    private void UpdateSettings(ConsoleKeyInfo? input)
    {
        _settings!.Render(_renderer.UIBuffer);

        if (input.HasValue)
        {
            if (!_settings.HandleInput(input.Value))
            {
                ApplySettingsToConfig();
                _gameState.CurrentPhase = GamePhase.MainMenu;
            }
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

    // ─── World Description ───────────────────────────────────────────

    private void UpdateWorldDescription(ConsoleKeyInfo? input)
    {
        _worldDesc!.Render(_renderer.UIBuffer);

        if (input.HasValue)
        {
            if (!_worldDesc.HandleInput(input.Value))
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
    }

    // ─── World Generation ────────────────────────────────────────────

    private void UpdateWorldGeneration(ConsoleKeyInfo? input)
    {
        // Start world generation on first entry
        if (_wgTask == null && !_wgDone && string.IsNullOrEmpty(_wgError))
        {
            _wgStatus = "Initializing...";
            StartWorldGeneration();
        }

        RenderWorldGenScreen();

        // Handle post-completion input
        if (_wgDone || !string.IsNullOrEmpty(_wgError))
        {
            if (input.HasValue && input.Value.Key == ConsoleKey.Enter)
            {
                if (_wgDone)
                {
                    _gameState.CurrentPhase = GamePhase.CharacterCreation;
                }
                else
                {
                    _gameState.CurrentPhase = GamePhase.MainMenu;
                }
            }
        }
    }

    private void StartWorldGeneration()
    {
        _aiTextLines.Clear();
        _toolCallLog.Clear();
        _wgRound = 0;
        _wgDone = false;
        _wgError = "";

        var ct = _aiCts.Token;

        _wgTask = Task.Run(async () =>
        {
            try
            {
                var worldBuilder = _agentOrchestrator.CreateWorldBuilder();

                await foreach (var evt in worldBuilder.BuildWorldWithEventsAsync(_gameState.WorldDescription, ct))
                {
                    _eventQueue.Enqueue(evt);
                }

                _eventQueue.Enqueue(new StreamEvent(StreamEventType.RoundComplete, "WORLD_GEN_DONE"));
            }
            catch (Exception ex)
            {
                _eventQueue.Enqueue(new StreamEvent(StreamEventType.Error, ex.Message));
            }
        }, ct);

        // Monitor task for completion
        _ = Task.Run(async () =>
        {
            try
            {
                await _wgTask!;
            }
            catch { /* already handled above */ }
        });
    }



    private void AppendTextDelta(string delta)
    {
        var parts = delta.Split('\n');
        for (var i = 0; i < parts.Length; i++)
        {
            if (i == 0)
            {
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

    // ─── Character Creation ──────────────────────────────────────────

    private void UpdateCharacterCreation(ConsoleKeyInfo? input)
    {
        if (_charCreation == null)
        {
            _charCreation = new CharacterCreationScreen();
            _charCreation.SetAvailableStats(new List<string> { "STR", "DEX", "CON", "INT", "WIS", "CHA" }, 10f);
        }

        _charCreation.Render(_renderer.UIBuffer);

        if (input.HasValue)
        {
            if (!_charCreation.HandleInput(input.Value))
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
    }

    // ─── Gameplay ────────────────────────────────────────────────────

    private void UpdateGameplay(ConsoleKeyInfo? input, float deltaTime)
    {
        // Start simulation and GM on first entry
        if (_gameMaster == null)
        {
            _simulation.Start();
            _gameState.AddMessage("You find yourself in a new world...");

            _gameMaster = _agentOrchestrator.CreateGameMaster();
            StartGMInitialization();
        }

        // Process player input
        if (input.HasValue)
        {
            HandleGameplayInput(input.Value);
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
                if (health != null)
                {
                    hp = health.CurrentHp;
                    maxHp = health.MaxHp;
                    stamina = health.CurrentStamina;
                    maxStamina = health.MaxStamina;
                }
            }
        }

        _renderer.RenderUI(playerName, hp, maxHp, stamina, maxStamina, location,
            _simulation.GameTime.ToString(), _gameState.MessageLog);

        // Show GM text if streaming
        string gmDisplay;
        bool isStreaming;
        lock (_gmText)
        {
            gmDisplay = _gmText.ToString();
            isStreaming = _gmStreaming;
        }

        if (gmDisplay.Length > 0)
        {
            var gmLines = gmDisplay.Split('\n').TakeLast(3).ToList();
            for (var i = 0; i < gmLines.Count; i++)
            {
                var line = gmLines[i];
                if (line.Length > _renderer.UIBuffer.Width - 4)
                    line = line[..(_renderer.UIBuffer.Width - 4)];
                _renderer.UIBuffer.Write(2, _renderer.UIBuffer.Height - 8 + i,
                    isStreaming ? line + "▌" : line, "#FFD700");
            }
        }
    }

    private void StartGMInitialization()
    {
        var ct = _aiCts.Token;
        var gm = _gameMaster!;

        // Start GM session synchronously (fast)
        gm.StartAsync(ct).GetAwaiter().GetResult();

        _gmInitTask = Task.Run(async () =>
        {
            try
            {
                var charDesc = _charCreation?.CharacterDescription ?? "A brave adventurer";
                var playerId = _gameState.CurrentPlayerId ?? "player_01";

                await foreach (var evt in gm.StreamWithEventsAsync(
                    $"A new player has entered the world. Here is their character description:\n\nPlayer ID: {playerId}\nDescription: {charDesc}\n\nPlease:\n1. READ the region data to find a suitable spawn point\n2. SPAWN the player at an appropriate location\n3. GIVE them starting items appropriate to their character\n4. CREATE an initial quest for them\n5. SPAWN any nearby creatures that make sense for the location", ct))
                {
                    _eventQueue.Enqueue(evt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GM initialization failed");
                lock (_gmText)
                {
                    _gmText.Append('\n').Append("[GM Error: ").Append(ex.Message).Append(']');
                    _gmStreaming = false;
                }
            }
        }, ct);
    }

    private void HandleGameplayInput(ConsoleKeyInfo key)
    {
        if (_gameState.CurrentPlayerId == null) return;

        var player = _entityManager.GetEntity(EntityId.From(_gameState.CurrentPlayerId));
        if (player == null) return;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
            {
                var moveSystem = _serviceProvider.GetRequiredService<MovementSystem>();
                moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, 0, -1).GetAwaiter().GetResult();
                break;
            }
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
            {
                var moveSystem = _serviceProvider.GetRequiredService<MovementSystem>();
                moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, 0, 1).GetAwaiter().GetResult();
                break;
            }
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
            {
                var moveSystem = _serviceProvider.GetRequiredService<MovementSystem>();
                moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, -1, 0).GetAwaiter().GetResult();
                break;
            }
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
            {
                var moveSystem = _serviceProvider.GetRequiredService<MovementSystem>();
                moveSystem.MoveEntityAsync(_gameState.CurrentPlayerId, 1, 0).GetAwaiter().GetResult();
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

    public void Dispose()
    {
        _aiCts.Dispose();
        GC.SuppressFinalize(this);
    }
}
