namespace AxiomRPG.Core.Math;

public readonly record struct Vector2(int X, int Y)
{
    public static Vector2 Zero => new(0, 0);
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 v, int scalar) => new(v.X * scalar, v.Y * scalar);

    public double DistanceTo(Vector2 other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return System.Math.Sqrt(dx * dx + dy * dy);
    }

    public int ManhattanDistanceTo(Vector2 other) => System.Math.Abs(X - other.X) + System.Math.Abs(Y - other.Y);

    public IEnumerable<Vector2> GetNeighbors8()
    {
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            yield return new Vector2(X + dx, Y + dy);
        }
    }

    public IEnumerable<Vector2> GetNeighbors4()
    {
        yield return new Vector2(X - 1, Y);
        yield return new Vector2(X + 1, Y);
        yield return new Vector2(X, Y - 1);
        yield return new Vector2(X, Y + 1);
    }
}
