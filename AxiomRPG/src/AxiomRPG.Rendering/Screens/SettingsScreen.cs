namespace AxiomRPG.Rendering.Screens;

public class SettingsScreen : IScreen
{
    public string ApiKey { get; private set; } = "";
    public string ApiBaseUrl { get; private set; } = "https://api.openai.com/v1";
    public string Model { get; private set; } = "gpt-4o";
    private int _selectedField;
    private bool _isEditing;
    private string _editBuffer = "";

    public void Initialize() { }

    public void Render(RenderBuffer buffer)
    {
        buffer.Clear();

        buffer.WriteCentered(2, "╔════ SETTINGS ════╗", "#FFD700");

        var fields = new[] { "API Key", "API Base URL", "Model" };

        for (var i = 0; i < fields.Length; i++)
        {
            var y = 6 + i * 4;
            buffer.Write(4, y, fields[i] + ":", i == _selectedField ? "#FFD700" : "#C0C0C0");

            // Show edit buffer when editing, otherwise show saved value
            string displayValue;
            if (i == _selectedField && _isEditing)
            {
                displayValue = _editBuffer;
            }
            else
            {
                displayValue = i switch
                {
                    0 => MaskString(ApiKey),
                    1 => ApiBaseUrl,
                    2 => Model,
                    _ => ""
                };
            }

            // Truncate to fit display width
            var maxLen = buffer.Width - 8;
            if (displayValue.Length > maxLen) displayValue = displayValue[..maxLen];

            buffer.Write(4, y + 1, displayValue, "#FFFFFF");

            // Show blinking cursor when editing
            if (i == _selectedField && _isEditing)
            {
                var cursorX = 4 + Math.Min(_editBuffer.Length, maxLen);
                if (cursorX < buffer.Width)
                {
                    buffer.SetCell(cursorX, y + 1, '_', "#FFD700");
                }
            }
        }

        buffer.WriteCentered(buffer.Height - 4, "Tab: Next Field  |  Enter: Edit/Confirm  |  Esc: Back", "#666666");

        // Show config hint
        buffer.WriteCentered(buffer.Height - 2, "Configure your OpenAI-compatible API endpoint", "#444444");
    }

    public bool HandleInput(ConsoleKeyInfo key)
    {
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
            if (key.KeyChar >= ' ' && _editBuffer.Length < 500)
            {
                _editBuffer += key.KeyChar;
                return true;
            }
            return true;
        }

        switch (key.Key)
        {
            case ConsoleKey.Tab:
                _selectedField = (_selectedField + 1) % 3;
                return true;
            case ConsoleKey.Enter:
                _isEditing = true;
                _editBuffer = _selectedField switch { 0 => ApiKey, 1 => ApiBaseUrl, 2 => Model, _ => "" };
                return true;
            case ConsoleKey.Escape:
                return false; // Back to menu
        }
        return true;
    }

    private void ApplyEdit()
    {
        switch (_selectedField)
        {
            case 0: ApiKey = _editBuffer; break;
            case 1: ApiBaseUrl = _editBuffer; break;
            case 2: Model = _editBuffer; break;
        }
        _editBuffer = "";
    }

    private static string MaskString(string s) => s.Length <= 8 ? s : s[..4] + new string('*', s.Length - 8) + s[^4..];
}
