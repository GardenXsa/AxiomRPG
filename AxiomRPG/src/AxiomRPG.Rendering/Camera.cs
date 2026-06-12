using AxiomRPG.Core.Math;

namespace AxiomRPG.Rendering;

public class Camera
{
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    public int WorldX { get; set; }  // Center of camera in world coordinates
    public int WorldY { get; set; }
    public int Zoom { get; set; } = 1; // 1 = normal, 2 = zoomed out (2x2 per tile)

    public Camera(int viewportWidth, int viewportHeight)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    public void CenterOn(int worldX, int worldY)
    {
        WorldX = worldX;
        WorldY = worldY;
    }

    public void Move(int dx, int dy)
    {
        WorldX += dx;
        WorldY += dy;
    }

    public Vector2 WorldToScreen(int worldX, int worldY)
    {
        var offsetX = WorldX - ViewportWidth / 2;
        var offsetY = WorldY - ViewportHeight / 2;
        return new Vector2(worldX - offsetX, worldY - offsetY);
    }

    public Vector2 ScreenToWorld(int screenX, int screenY)
    {
        var offsetX = WorldX - ViewportWidth / 2;
        var offsetY = WorldY - ViewportHeight / 2;
        return new Vector2(screenX + offsetX, screenY + offsetY);
    }

    public Rect GetVisibleWorldRect()
    {
        var halfW = ViewportWidth / 2;
        var halfH = ViewportHeight / 2;
        return new Rect(WorldX - halfW, WorldY - halfH, ViewportWidth, ViewportHeight);
    }
}
