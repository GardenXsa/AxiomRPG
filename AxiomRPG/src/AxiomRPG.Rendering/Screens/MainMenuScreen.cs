using AxiomRPG.Core.Types;

namespace AxiomRPG.Rendering.Screens;

public class MainMenuScreen : IScreen
{
    private int _selectedIndex;
    private readonly string[] _menuItems = { "New Game", "Load Game", "Settings", "Quit" };

    public void Initialize() { }

    public void Render(RenderBuffer buffer)
    {
        buffer.Clear();

        // Title
        buffer.WriteCentered(3, "╔══════════════════════════════════════════╗", "#FFD700");
        buffer.WriteCentered(4, "║            A X I O M   R P G            ║", "#FFD700");
        buffer.WriteCentered(5, "║         AI-Driven ASCII RPG World        ║", "#AAAAAA");
        buffer.WriteCentered(6, "╚══════════════════════════════════════════╝", "#FFD700");

        // ASCII art — a sword
        buffer.WriteCentered(9, "        /| ________________", "#808080");
        buffer.WriteCentered(10, "O|===|* > _______________/ ", "#C0C0C0");
        buffer.WriteCentered(11, "        \\|               ", "#808080");

        // Menu items
        for (var i = 0; i < _menuItems.Length; i++)
        {
            var y = 16 + i * 2;
            if (i == _selectedIndex)
            {
                buffer.WriteCentered(y, $">> {_menuItems[i]} <<", "#FFD700");
            }
            else
            {
                buffer.WriteCentered(y, $"   {_menuItems[i]}   ", "#C0C0C0");
            }
        }

        // Footer
        buffer.WriteCentered(buffer.Height - 3, "Use Arrow Keys to navigate, Enter to select", "#666666");
        buffer.WriteCentered(buffer.Height - 2, "A game powered by AI", "#444444");
    }

    public bool HandleInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                _selectedIndex = (_selectedIndex - 1 + _menuItems.Length) % _menuItems.Length;
                return true;
            case ConsoleKey.DownArrow:
                _selectedIndex = (_selectedIndex + 1) % _menuItems.Length;
                return true;
            case ConsoleKey.Enter:
                return false; // Signal: selection made
        }
        return true;
    }

    public GamePhase GetSelectedPhase() => _selectedIndex switch
    {
        0 => GamePhase.WorldDescription,
        1 => GamePhase.Gameplay, // Load game
        2 => GamePhase.Settings,
        3 => GamePhase.GameOver, // Quit
        _ => GamePhase.MainMenu
    };
}
