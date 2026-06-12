namespace AxiomRPG.Rendering.Screens;

public class CharacterCreationScreen : IScreen
{
    public string CharacterName { get; private set; } = "";
    public string CharacterDescription { get; private set; } = "";
    public Dictionary<string, float> AllocatedStats { get; private set; } = new();
    private int _selectedField;
    private bool _isEditing;
    private string _editBuffer = "";

    public void SetAvailableStats(List<string> statNames, float pointsPerStat = 10f)
    {
        foreach (var name in statNames)
        {
            AllocatedStats[name] = pointsPerStat;
        }
    }

    public void Initialize() { }

    public void Render(RenderBuffer buffer)
    {
        buffer.Clear();

        buffer.WriteCentered(1, "═══ Create Your Character ═══", "#FFD700");

        // Name
        buffer.Write(4, 4, "Name:", _selectedField == 0 ? "#FFD700" : "#C0C0C0");
        buffer.Write(4, 5, _isEditing && _selectedField == 0 ? _editBuffer + "_" : CharacterName, "#FFFFFF");

        // Description
        buffer.Write(4, 7, "Description:", _selectedField == 1 ? "#FFD700" : "#C0C0C0");
        var descLine = _isEditing && _selectedField == 1 ? _editBuffer : CharacterDescription;
        var truncatedDesc = descLine.Length > buffer.Width - 8 ? descLine[..(buffer.Width - 8)] : descLine;
        buffer.Write(4, 8, truncatedDesc, "#FFFFFF");

        // Stats
        buffer.Write(4, 10, "Stats:", "#FFD700");
        var y = 11;
        foreach (var (statName, value) in AllocatedStats)
        {
            var barLen = (int)value;
            var bar = new string('█', barLen) + new string('░', 20 - barLen);
            buffer.Write(4, y, $"{statName,-8}", "#AAAAAA");
            buffer.Write(14, y, bar, "#44FF44");
            buffer.Write(36, y, $"{value:F0}", "#FFFFFF");
            y++;
        }

        buffer.WriteCentered(buffer.Height - 3, "Tab: Next  |  +/-: Adjust Stats  |  Enter: Edit/Confirm  |  Esc: Back", "#666666");
    }

    public bool HandleInput(ConsoleKeyInfo key)
    {
        var fieldCount = 2 + AllocatedStats.Count;

        if (_isEditing)
        {
            if (key.Key == ConsoleKey.Enter)
            {
                _isEditing = false;
                ApplyEdit();
                return true;
            }
            if (key.Key == ConsoleKey.Escape)
            {
                _isEditing = false;
                _editBuffer = "";
                return true;
            }
            if (key.Key == ConsoleKey.Backspace && _editBuffer.Length > 0)
            {
                _editBuffer = _editBuffer[..^1];
                return true;
            }
            if (key.KeyChar >= ' ')
            {
                _editBuffer += key.KeyChar;
                return true;
            }
            return true;
        }

        switch (key.Key)
        {
            case ConsoleKey.Tab:
                _selectedField = (_selectedField + 1) % fieldCount;
                return true;
            case ConsoleKey.Enter:
                if (_selectedField < 2)
                {
                    _isEditing = true;
                    _editBuffer = _selectedField switch { 0 => CharacterName, 1 => CharacterDescription, _ => "" };
                }
                return true;
            case ConsoleKey.Escape:
                return false;
            case ConsoleKey.Add:
            case ConsoleKey.OemPlus:
                AdjustStat(1);
                return true;
            case ConsoleKey.Subtract:
            case ConsoleKey.OemMinus:
                AdjustStat(-1);
                return true;
        }
        return true;
    }

    private void AdjustStat(int delta)
    {
        if (_selectedField < 2) return;
        var statIndex = _selectedField - 2;
        var statName = AllocatedStats.Keys.ElementAtOrDefault(statIndex);
        if (statName == null) return;
        AllocatedStats[statName] = Math.Clamp(AllocatedStats[statName] + delta, 1, 20);
    }

    private void ApplyEdit()
    {
        switch (_selectedField)
        {
            case 0: CharacterName = _editBuffer; break;
            case 1: CharacterDescription = _editBuffer; break;
        }
        _editBuffer = "";
    }
}
