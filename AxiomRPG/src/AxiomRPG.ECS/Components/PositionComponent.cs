using AxiomRPG.Core.Math;
using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record PositionComponent(
    int X,
    int Y,
    string RegionId,
    string? ZoneId = null,
    string? ChunkId = null,
    int Z = 0  // Z-level for multi-floor buildings
) : IComponent
{
    public string ComponentType => "position";
    public Vector2 AsVector2 => new(X, Y);
}
