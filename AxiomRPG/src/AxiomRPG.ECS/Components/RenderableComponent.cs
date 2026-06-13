using AxiomRPG.Core.Types;
using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record RenderableComponent : IComponent
{
    public string ComponentType => "renderable";
    public AsciiTile Tile { get; init; }
    public int RenderLayer { get; init; } = 0; // 0=terrain, 1=items, 2=creatures, 3=player, 4=effects
    public bool IsVisible { get; set; } = true;
}
