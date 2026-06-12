using AxiomRPG.Core.Types;

namespace AxiomRPG.Game;

public class GameState
{
    public GamePhase CurrentPhase { get; set; } = GamePhase.MainMenu;
    public string? CurrentPlayerId { get; set; }
    public string? CurrentPlanetId { get; set; }
    public bool IsRunning { get; set; } = true;
    public List<string> MessageLog { get; } = new();
    public string WorldDescription { get; set; } = "";
    public bool WorldGenerationComplete { get; set; } = false;

    public void AddMessage(string message)
    {
        MessageLog.Add(message);
        if (MessageLog.Count > 100) MessageLog.RemoveAt(0);
    }
}
