using AxiomRPG.Core.Math;
using AxiomRPG.Core.Types;
using AxiomRPG.ECS;
using AxiomRPG.Components;
using AxiomRPG.World;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.Rendering;

public class ASCIIRenderer
{
    private readonly EntityManager _entityManager;
    private readonly WorldManager _worldManager;
    private readonly ILogger<ASCIIRenderer> _logger;

    public RenderBuffer WorldBuffer { get; }
    public RenderBuffer UIBuffer { get; }
    public Camera Camera { get; }

    public int ScreenWidth { get; }
    public int ScreenHeight { get; }

    public ASCIIRenderer(
        EntityManager entityManager,
        WorldManager worldManager,
        ILogger<ASCIIRenderer> logger,
        int screenWidth = 80,
        int screenHeight = 50
    )
    {
        _entityManager = entityManager;
        _worldManager = worldManager;
        _logger = logger;
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;

        // World takes left portion, UI takes right portion
        var worldWidth = screenWidth - 20; // 20 chars for UI panel
        var worldHeight = screenHeight - 5;  // 5 lines for bottom log

        WorldBuffer = new RenderBuffer(worldWidth, worldHeight);
        UIBuffer = new RenderBuffer(screenWidth, screenHeight);
        Camera = new Camera(worldWidth, worldHeight);
    }

    public void RenderWorld()
    {
        WorldBuffer.Clear();

        var visibleRect = Camera.GetVisibleWorldRect();

        // Render terrain
        var startChunkX = visibleRect.Left / Chunk.ChunkSize;
        var startChunkY = visibleRect.Top / Chunk.ChunkSize;
        var endChunkX = (visibleRect.Right / Chunk.ChunkSize) + 1;
        var endChunkY = (visibleRect.Bottom / Chunk.ChunkSize) + 1;

        for (var cy = startChunkY; cy <= endChunkY; cy++)
        for (var cx = startChunkX; cx <= endChunkX; cx++)
        {
            var chunk = _worldManager.GetLoadedChunk(cx, cy);
            if (chunk == null) continue;

            for (var ty = 0; ty < Chunk.ChunkSize; ty++)
            for (var tx = 0; tx < Chunk.ChunkSize; tx++)
            {
                var worldX = cx * Chunk.ChunkSize + tx;
                var worldY = cy * Chunk.ChunkSize + ty;

                if (!visibleRect.Contains(new Vector2(worldX, worldY))) continue;

                var screenPos = Camera.WorldToScreen(worldX, worldY);
                var tile = chunk.GetTile(tx, ty);

                WorldBuffer.SetCell(screenPos.X, screenPos.Y, tile.Character, tile.ForegroundColor, tile.BackgroundColor);
            }
        }

        // Render entities
        var renderableEntities = _entityManager.QueryEntitiesWith<RenderableComponent, PositionComponent>();
        foreach (var entity in renderableEntities)
        {
            var renderable = _entityManager.GetComponent<RenderableComponent>(entity.Id);
            var position = _entityManager.GetComponent<PositionComponent>(entity.Id);
            if (renderable == null || position == null || !renderable.IsVisible) continue;

            if (!visibleRect.Contains(new Vector2(position.X, position.Y))) continue;

            var screenPos = Camera.WorldToScreen(position.X, position.Y);
            WorldBuffer.SetCell(screenPos.X, screenPos.Y, renderable.Tile.Character, renderable.Tile.ForegroundColor, renderable.Tile.BackgroundColor);
        }
    }

    public void RenderUI(string playerName, float hp, float maxHp, float stamina, float maxStamina, string location, string timeOfDay, List<string> messageLog)
    {
        UIBuffer.Clear();

        // World viewport frame
        var worldWidth = ScreenWidth - 20;
        var worldHeight = ScreenHeight - 5;
        UIBuffer.DrawBox(0, 0, worldWidth + 2, worldHeight + 2, "#808080");

        // Right panel — character info
        var panelX = worldWidth + 2;
        UIBuffer.DrawBox(panelX, 0, 18, ScreenHeight, "#808080");
        UIBuffer.Write(panelX + 2, 1, playerName, "#FFD700");

        // HP bar
        UIBuffer.Write(panelX + 2, 3, "HP:", "#FF4444");
        var hpBarLen = 12;
        var hpFilled = (int)(hpBarLen * hp / Math.Max(1, maxHp));
        for (var i = 0; i < hpBarLen; i++)
        {
            var c = i < hpFilled ? '█' : '░';
            UIBuffer.SetCell(panelX + 6 + i, 3, c, i < hpFilled ? "#FF4444" : "#441111");
        }
        UIBuffer.Write(panelX + 2, 4, $"{hp:F0}/{maxHp:F0}", "#FF4444");

        // Stamina bar
        UIBuffer.Write(panelX + 2, 6, "SP:", "#44FF44");
        var spFilled = (int)(hpBarLen * stamina / Math.Max(1, maxStamina));
        for (var i = 0; i < hpBarLen; i++)
        {
            var c = i < spFilled ? '█' : '░';
            UIBuffer.SetCell(panelX + 6 + i, 6, c, i < spFilled ? "#44FF44" : "#114411");
        }
        UIBuffer.Write(panelX + 2, 7, $"{stamina:F0}/{maxStamina:F0}", "#44FF44");

        // Location & time
        UIBuffer.Write(panelX + 2, 9, "Location:", "#AAAAAA");
        var locationDisplay = location.Length > 14 ? location[..14] : location;
        UIBuffer.Write(panelX + 2, 10, locationDisplay, "#FFFFFF");
        UIBuffer.Write(panelX + 2, 12, "Time:", "#AAAAAA");
        UIBuffer.Write(panelX + 2, 13, timeOfDay, "#FFFFFF");

        // Bottom — message log
        var logY = worldHeight + 2;
        UIBuffer.DrawBox(0, logY, ScreenWidth, 5, "#808080");
        for (var i = 0; i < Math.Min(messageLog.Count, 3); i++)
        {
            var msg = messageLog[i];
            var truncated = msg.Length > ScreenWidth - 4 ? msg[..(ScreenWidth - 4)] : msg;
            UIBuffer.Write(2, logY + 1 + i, truncated, "#C0C0C0");
        }
    }

    /// <summary>
    /// Convert the render buffer to ANSI escape sequences for terminal output
    /// </summary>
    public string ToAnsiString()
    {
        var sb = new System.Text.StringBuilder();
        // Reset and move cursor to top-left
        sb.Append("\u001b[2J\u001b[H");

        for (var y = 0; y < UIBuffer.Height; y++)
        {
            for (var x = 0; x < UIBuffer.Width; x++)
            {
                var cell = UIBuffer.GetCell(x, y);
                var fg = ParseAnsiColor(cell.Foreground);
                var bg = ParseAnsiColor(cell.Background);
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "\u001b[{0};{1}m{2}", fg, bg + 10, cell.Character));
            }
            sb.AppendLine();
        }
        sb.Append("\u001b[0m"); // Reset
        return sb.ToString();
    }

    /// <summary>
    /// Convert to plain text (no colors) for simple console output
    /// </summary>
    public string ToPlainText()
    {
        var sb = new System.Text.StringBuilder();
        for (var y = 0; y < UIBuffer.Height; y++)
        {
            for (var x = 0; x < UIBuffer.Width; x++)
            {
                sb.Append(UIBuffer.GetCell(x, y).Character);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static int ParseAnsiColor(string hexColor)
    {
        // Map common colors to ANSI 256-color codes
        return hexColor.ToUpperInvariant() switch
        {
            "#000000" => 0,   // Black
            "#FF0000" => 1,   // Red
            "#00FF00" => 2,   // Green
            "#FFFF00" => 3,   // Yellow
            "#0000FF" => 4,   // Blue
            "#FF00FF" => 5,   // Magenta
            "#00FFFF" => 6,   // Cyan
            "#FFFFFF" => 7,   // White
            "#808080" => 8,   // Bright Black (Gray)
            "#FF4444" => 9,   // Bright Red
            "#44FF44" => 10,  // Bright Green
            "#FFD700" => 11,  // Bright Yellow (Gold)
            "#4169E1" => 12,  // Bright Blue
            "#FF69B4" => 13,  // Bright Magenta (Hot Pink variant)
            "#00CED1" => 14,  // Bright Cyan
            "#C0C0C0" => 15,  // Bright White
            _ => 7            // Default white
        };
    }
}
