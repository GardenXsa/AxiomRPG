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

// Configure console
Console.Title = "AxiomRPG — AI-Driven ASCII RPG";
Console.CursorVisible = false;
try { Console.SetWindowSize(82, 52); } catch { }
try { Console.SetBufferSize(82, 52); } catch { }

// Build DI container
var dataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data"));

var services = new ServiceCollection();

// Logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
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

// Run the game
var gameLoop = serviceProvider.GetRequiredService<GameLoop>();

try
{
    await gameLoop.RunAsync();
}
catch (OperationCanceledException)
{
    // Normal shutdown
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
}
finally
{
    Console.CursorVisible = true;
    Console.ResetColor();
}
