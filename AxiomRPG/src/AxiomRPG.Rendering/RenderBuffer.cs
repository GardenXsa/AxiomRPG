using AxiomRPG.Core.Types;

namespace AxiomRPG.Rendering;

public class RenderBuffer
{
    public int Width { get; }
    public int Height { get; }

    private readonly AsciiCell[,] _cells;

    public RenderBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new AsciiCell[width, height];
        Clear();
    }

    public void Clear()
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
            _cells[x, y] = new AsciiCell(' ', "#C0C0C0", "#000000");
    }

    public void SetCell(int x, int y, char character, string foreground, string background = "#000000")
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        _cells[x, y] = new AsciiCell(character, foreground, background);
    }

    public AsciiCell GetCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return new AsciiCell(' ', "#000000", "#000000");
        return _cells[x, y];
    }

    public void Write(int x, int y, string text, string foreground = "#C0C0C0", string background = "#000000")
    {
        for (var i = 0; i < text.Length && x + i < Width; i++)
        {
            SetCell(x + i, y, text[i], foreground, background);
        }
    }

    public void WriteCentered(int y, string text, string foreground = "#C0C0C0", string background = "#000000")
    {
        var x = (Width - text.Length) / 2;
        Write(Math.Max(0, x), y, text, foreground, background);
    }

    public void DrawBox(int x, int y, int width, int height, string color = "#808080")
    {
        // Top-left corner
        SetCell(x, y, '╔', color);
        // Top-right corner
        SetCell(x + width - 1, y, '╗', color);
        // Bottom-left corner
        SetCell(x, y + height - 1, '╚', color);
        // Bottom-right corner
        SetCell(x + width - 1, y + height - 1, '╝', color);

        // Horizontal lines
        for (var i = 1; i < width - 1; i++)
        {
            SetCell(x + i, y, '═', color);
            SetCell(x + i, y + height - 1, '═', color);
        }
        // Vertical lines
        for (var j = 1; j < height - 1; j++)
        {
            SetCell(x, y + j, '║', color);
            SetCell(x + width - 1, y + j, '║', color);
        }
    }

    public void FillRect(int x, int y, int width, int height, char c = ' ', string foreground = "#C0C0C0", string background = "#000000")
    {
        for (var dy = 0; dy < height; dy++)
        for (var dx = 0; dx < width; dx++)
            SetCell(x + dx, y + dy, c, foreground, background);
    }
}

public record AsciiCell(char Character, string Foreground, string Background);
