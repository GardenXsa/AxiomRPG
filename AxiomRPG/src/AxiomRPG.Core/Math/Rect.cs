namespace AxiomRPG.Core.Math;

public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Right => X + Width - 1;
    public int Top => Y;
    public int Bottom => Y + Height - 1;
    public Vector2 Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Vector2 point) =>
        point.X >= X && point.X < X + Width && point.Y >= Y && point.Y < Y + Height;

    public bool Intersects(Rect other) =>
        Left <= other.Right && Right >= other.Left && Top <= other.Bottom && Bottom >= other.Top;

    public IEnumerable<Vector2> GetAllPoints()
    {
        for (var y = Y; y < Y + Height; y++)
        for (var x = X; x < X + Width; x++)
            yield return new Vector2(x, y);
    }
}
