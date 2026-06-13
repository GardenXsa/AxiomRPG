using AxiomRPG.AI.Extensions;
using AxiomRPG.Core.Extensions;
using AxiomRPG.Data.Extensions;
using AxiomRPG.Game;
using AxiomRPG.Rendering.Extensions;
using AxiomRPG.Simulation.Extensions;
using AxiomRPG.ToolAPI;
using AxiomRPG.ToolAPI.Extensions;
using AxiomRPG.World.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Data path: look for data/ next to executable first, then dev fallback
var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dataPath))
{
    dataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data"));
}

// Build DI container
var services = new ServiceCollection();

// Logging — write to file, not console (we're in Raylib window mode)
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Register all modules
services.AddAxiomCore();
services.AddAxiomData(dataPath);
services.AddAxiomWorld();
services.AddAxiomSimulation();
services.AddAxiomToolAPI();
services.AddAxiomAI();
services.AddAxiomRendering(80, 50);

// Game-specific
services.AddSingleton<GameState>();
services.AddSingleton<GameLoop>();

var serviceProvider = services.BuildServiceProvider();

// Wire up tool handlers to the dispatcher after all services are registered
ToolRegistry.BuildDispatcher(serviceProvider);

// Initialize Raylib window (must be on main thread)
using var window = new RaylibWindow();
window.Init();

// Initialize game loop
var gameLoop = serviceProvider.GetRequiredService<GameLoop>();
gameLoop.Init();

try
{
    // Main game loop — synchronous, Raylib runs on this thread
    while (!window.ShouldClose && gameLoop.IsRunning)
    {
        var input = window.PollInput();
        gameLoop.Update(input);
        window.Render(gameLoop.GetRenderBuffer());
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
}
finally
{
    gameLoop.Shutdown();
}
