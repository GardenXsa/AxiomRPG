namespace AxiomRPG.Rendering.Screens;

public class WorldDescriptionScreen : IScreen
{
    public string WorldDescription { get; private set; } = "";
    private bool _isEditing = true;
    private readonly List<string> _lines = new() { "" };
    private int _cursorLine;
    private int _cursorCol;

    public void Initialize() { }

    public void Render(RenderBuffer buffer)
    {
        buffer.Clear();

        buffer.WriteCentered(1, "═══ Describe Your World ═══", "#FFD700");
        buffer.WriteCentered(3, "Describe the world you want the AI to create.", "#AAAAAA");
        buffer.WriteCentered(4, "Be as detailed as you like — biomes, cultures, magic, conflicts...", "#AAAAAA");

        // Text area
        buffer.DrawBox(2, 6, buffer.Width - 4, buffer.Height - 10, "#808080");

        for (var i = 0; i < _lines.Count && i < buffer.Height - 12; i++)
        {
            var y = 7 + i;
            var line = _lines[i];
            buffer.Write(3, y, line, "#FFFFFF");
            if (i == _cursorLine && _isEditing)
            {
                buffer.SetCell(3 + _cursorCol, y, '_', "#FFD700");
            }
        }

        buffer.WriteCentered(buffer.Height - 3, "Enter: New Line  |  Ctrl+Enter: Submit  |  Esc: Cancel", "#666666");
    }

    public bool HandleInput(ConsoleKeyInfo key)
    {
        if (!_isEditing) return false;

        if (key.Key == ConsoleKey.Escape) return false;

        if (key.Key == ConsoleKey.Enter && key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            WorldDescription = string.Join("\n", _lines);
            _isEditing = false;
            return false; // Signal: submit
        }

        if (key.Key == ConsoleKey.Enter)
        {
            _lines.Insert(_cursorLine + 1, _lines[_cursorLine][_cursorCol..]);
            _lines[_cursorLine] = _lines[_cursorLine][.._cursorCol];
            _cursorLine++;
            _cursorCol = 0;
            return true;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (_cursorCol > 0)
            {
                _lines[_cursorLine] = _lines[_cursorLine][..(_cursorCol - 1)] + _lines[_cursorLine][_cursorCol..];
                _cursorCol--;
            }
            else if (_cursorLine > 0)
            {
                _cursorCol = _lines[_cursorLine - 1].Length;
                _lines[_cursorLine - 1] += _lines[_cursorLine];
                _lines.RemoveAt(_cursorLine);
                _cursorLine--;
            }
            return true;
        }

        if (key.KeyChar >= ' ' && _cursorCol < 74) // buffer width hint approximation
        {
            _lines[_cursorLine] = _lines[_cursorLine][.._cursorCol] + key.KeyChar + _lines[_cursorLine][_cursorCol..];
            _cursorCol++;
            return true;
        }

        return true;
    }
}
