using System.Runtime.InteropServices;
using Raylib_cs;
using AxiomRPG.Rendering;

using RlColor = Raylib_cs.Color;
using RlVector2 = System.Numerics.Vector2;

namespace AxiomRPG.Game;

/// <summary>
/// Raylib-based window that renders the ASCII RenderBuffer as colored text
/// and provides keyboard input mapped to ConsoleKeyInfo.
/// </summary>
public class RaylibWindow : IDisposable
{
    private const int GridWidth = 80;
    private const int GridHeight = 50;
    private const int FontSize = 14;

    private int _cellWidth = 8;
    private int _cellHeight = 14;
    private Font _font;
    private bool _fontLoaded;
    private bool _initialized;

    // Pre-allocated char buffer to avoid stackalloc in loop
    private readonly char[] _charBuf = new char[1];

    public void Init()
    {
        // Try loading font before window (for measurement)
        TryLoadFont();
        MeasureCellSize();

        var windowWidth = GridWidth * _cellWidth;
        var windowHeight = GridHeight * _cellHeight;

        Raylib.InitWindow(windowWidth, windowHeight, "AxiomRPG - AI-Driven ASCII RPG");
        Raylib.SetTargetFPS(30);

        // Font needs GL context — reload after InitWindow
        if (_fontLoaded)
        {
            TryLoadFont();
        }

        _initialized = true;
    }

    private void TryLoadFont()
    {
        _fontLoaded = false;

        // Try to load from data directory next to executable
        var dataFontPath = Path.Combine(AppContext.BaseDirectory, "data", "DejaVuSansMono.ttf");
        if (File.Exists(dataFontPath))
        {
            try
            {
                _font = Raylib.LoadFontEx(dataFontPath, FontSize, null, 512);
                _fontLoaded = true;
                return;
            }
            catch
            {
                // Fall through
            }
        }

        // Try system fonts
        var systemFonts = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { @"C:\Windows\Fonts\consola.ttf", @"C:\Windows\Fonts\cascadia.ttf", @"C:\Windows\Fonts\lucon.ttf" }
            : new[] { "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf", "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf" };

        foreach (var path in systemFonts)
        {
            if (File.Exists(path))
            {
                try
                {
                    _font = Raylib.LoadFontEx(path, FontSize, null, 512);
                    _fontLoaded = true;
                    return;
                }
                catch
                {
                    // Fall through
                }
            }
        }

        // Fallback: use default Raylib font
        _font = Raylib.GetFontDefault();
        _fontLoaded = false;
    }

    private void MeasureCellSize()
    {
        if (_fontLoaded)
        {
            var size = Raylib.MeasureTextEx(_font, "M", FontSize, 1.0f);
            _cellWidth = Math.Max(4, (int)Math.Ceiling(size.X));
            _cellHeight = Math.Max(6, (int)Math.Ceiling(size.Y));
        }
        else
        {
            _cellWidth = 8;
            _cellHeight = FontSize;
        }
    }

    public bool ShouldClose => !_initialized || Raylib.WindowShouldClose();

    public void Render(RenderBuffer buffer)
    {
        if (!_initialized) return;

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new RlColor(0, 0, 0, 255));

        var maxW = Math.Min(buffer.Width, GridWidth);
        var maxH = Math.Min(buffer.Height, GridHeight);

        for (var y = 0; y < maxH; y++)
        {
            for (var x = 0; x < maxW; x++)
            {
                var cell = buffer.GetCell(x, y);

                // Draw background if not black
                if (cell.Background != "#000000")
                {
                    var bgColor = ParseHexColor(cell.Background);
                    Raylib.DrawRectangle(x * _cellWidth, y * _cellHeight, _cellWidth, _cellHeight, bgColor);
                }

                // Draw character
                if (cell.Character != ' ')
                {
                    var fgColor = ParseHexColor(cell.Foreground);
                    var posX = x * _cellWidth;
                    var posY = y * _cellHeight;

                    if (_fontLoaded)
                    {
                        _charBuf[0] = cell.Character;
                        var text = new string(_charBuf);
                        Raylib.DrawTextEx(_font, text, new RlVector2(posX, posY), FontSize, 1.0f, fgColor);
                    }
                    else
                    {
                        Raylib.DrawText(cell.Character.ToString(), posX, posY, FontSize, fgColor);
                    }
                }
            }
        }

        Raylib.EndDrawing();
    }

    /// <summary>
    /// Poll keyboard input and map to ConsoleKeyInfo. Returns null if no input this frame.
    /// </summary>
    public ConsoleKeyInfo? PollInput()
    {
        if (!_initialized) return null;

        // First: character input (typed characters like letters, numbers, symbols)
        var charCode = Raylib.GetCharPressed();
        if (charCode > 0)
        {
            var c = (char)charCode;
            var consoleKey = MapCharToConsoleKey(c);
            var (shift, alt, ctrl) = GetModifiers();
            return new ConsoleKeyInfo(c, consoleKey, shift, alt, ctrl);
        }

        // Second: special keys (arrows, enter, tab, etc.)
        var keyCode = Raylib.GetKeyPressed();
        if (keyCode > 0)
        {
            var kbKey = (KeyboardKey)keyCode;
            var consoleKey = MapRaylibKeyToConsoleKey(kbKey);
            if (consoleKey == ConsoleKey.None) return null;
            var (shift, alt, ctrl) = GetModifiers();
            return new ConsoleKeyInfo('\0', consoleKey, shift, alt, ctrl);
        }

        return null;
    }

    private static (bool shift, bool alt, bool ctrl) GetModifiers()
    {
        return (
            Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift),
            Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt),
            Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl)
        );
    }

    private static ConsoleKey MapCharToConsoleKey(char c) => c switch
    {
        >= 'a' and <= 'z' => ConsoleKey.A + (c - 'a'),
        >= 'A' and <= 'Z' => ConsoleKey.A + (c - 'A'),
        >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
        ' ' => ConsoleKey.Spacebar,
        '.' => ConsoleKey.OemPeriod,
        ',' => ConsoleKey.OemComma,
        '/' => ConsoleKey.Oem2,
        ';' => ConsoleKey.Oem1,
        '\'' => ConsoleKey.Oem7,
        '[' => ConsoleKey.Oem4,
        ']' => ConsoleKey.Oem6,
        '\\' => ConsoleKey.Oem5,
        '-' => ConsoleKey.OemMinus,
        '=' => ConsoleKey.OemPlus,
        '`' => ConsoleKey.Oem3,
        _ => ConsoleKey.None
    };

    private static ConsoleKey MapRaylibKeyToConsoleKey(KeyboardKey key) => key switch
    {
        KeyboardKey.Enter => ConsoleKey.Enter,
        KeyboardKey.Backspace => ConsoleKey.Backspace,
        KeyboardKey.Tab => ConsoleKey.Tab,
        KeyboardKey.Escape => ConsoleKey.Escape,
        KeyboardKey.Space => ConsoleKey.Spacebar,
        KeyboardKey.Up => ConsoleKey.UpArrow,
        KeyboardKey.Down => ConsoleKey.DownArrow,
        KeyboardKey.Left => ConsoleKey.LeftArrow,
        KeyboardKey.Right => ConsoleKey.RightArrow,
        KeyboardKey.PageUp => ConsoleKey.PageUp,
        KeyboardKey.PageDown => ConsoleKey.PageDown,
        KeyboardKey.Home => ConsoleKey.Home,
        KeyboardKey.End => ConsoleKey.End,
        KeyboardKey.Insert => ConsoleKey.Insert,
        KeyboardKey.Delete => ConsoleKey.Delete,
        KeyboardKey.F1 => ConsoleKey.F1,
        KeyboardKey.F2 => ConsoleKey.F2,
        KeyboardKey.F3 => ConsoleKey.F3,
        KeyboardKey.F4 => ConsoleKey.F4,
        KeyboardKey.F5 => ConsoleKey.F5,
        KeyboardKey.F6 => ConsoleKey.F6,
        KeyboardKey.F7 => ConsoleKey.F7,
        KeyboardKey.F8 => ConsoleKey.F8,
        KeyboardKey.F9 => ConsoleKey.F9,
        KeyboardKey.F10 => ConsoleKey.F10,
        KeyboardKey.F11 => ConsoleKey.F11,
        KeyboardKey.F12 => ConsoleKey.F12,
        KeyboardKey.Minus => ConsoleKey.OemMinus,
        KeyboardKey.Equal => ConsoleKey.OemPlus,
        KeyboardKey.LeftBracket => ConsoleKey.Oem4,
        KeyboardKey.RightBracket => ConsoleKey.Oem6,
        KeyboardKey.Backslash => ConsoleKey.Oem5,
        KeyboardKey.Semicolon => ConsoleKey.Oem1,
        KeyboardKey.Apostrophe => ConsoleKey.Oem7,
        KeyboardKey.Comma => ConsoleKey.OemComma,
        KeyboardKey.Period => ConsoleKey.OemPeriod,
        KeyboardKey.Slash => ConsoleKey.Oem2,
        KeyboardKey.Grave => ConsoleKey.Oem3,
        KeyboardKey.A => ConsoleKey.A,
        KeyboardKey.B => ConsoleKey.B,
        KeyboardKey.C => ConsoleKey.C,
        KeyboardKey.D => ConsoleKey.D,
        KeyboardKey.E => ConsoleKey.E,
        KeyboardKey.F => ConsoleKey.F,
        KeyboardKey.G => ConsoleKey.G,
        KeyboardKey.H => ConsoleKey.H,
        KeyboardKey.I => ConsoleKey.I,
        KeyboardKey.J => ConsoleKey.J,
        KeyboardKey.K => ConsoleKey.K,
        KeyboardKey.L => ConsoleKey.L,
        KeyboardKey.M => ConsoleKey.M,
        KeyboardKey.N => ConsoleKey.N,
        KeyboardKey.O => ConsoleKey.O,
        KeyboardKey.P => ConsoleKey.P,
        KeyboardKey.Q => ConsoleKey.Q,
        KeyboardKey.R => ConsoleKey.R,
        KeyboardKey.S => ConsoleKey.S,
        KeyboardKey.T => ConsoleKey.T,
        KeyboardKey.U => ConsoleKey.U,
        KeyboardKey.V => ConsoleKey.V,
        KeyboardKey.W => ConsoleKey.W,
        KeyboardKey.X => ConsoleKey.X,
        KeyboardKey.Y => ConsoleKey.Y,
        KeyboardKey.Z => ConsoleKey.Z,
        KeyboardKey.Zero => ConsoleKey.D0,
        KeyboardKey.One => ConsoleKey.D1,
        KeyboardKey.Two => ConsoleKey.D2,
        KeyboardKey.Three => ConsoleKey.D3,
        KeyboardKey.Four => ConsoleKey.D4,
        KeyboardKey.Five => ConsoleKey.D5,
        KeyboardKey.Six => ConsoleKey.D6,
        KeyboardKey.Seven => ConsoleKey.D7,
        KeyboardKey.Eight => ConsoleKey.D8,
        KeyboardKey.Nine => ConsoleKey.D9,
        _ => ConsoleKey.None
    };

    internal static RlColor ParseHexColor(string hex)
    {
        if (hex.StartsWith('#') && hex.Length == 7)
        {
            var r = Convert.ToByte(hex.Substring(1, 2), 16);
            var g = Convert.ToByte(hex.Substring(3, 2), 16);
            var b = Convert.ToByte(hex.Substring(5, 2), 16);
            return new RlColor(r, g, b, byte.MaxValue);
        }
        return new RlColor(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
    }

    public void Dispose()
    {
        if (_initialized)
        {
            if (_fontLoaded)
            {
                Raylib.UnloadFont(_font);
                _fontLoaded = false;
            }
            Raylib.CloseWindow();
            _initialized = false;
        }
        GC.SuppressFinalize(this);
    }
}
