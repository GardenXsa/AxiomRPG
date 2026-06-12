using AxiomRPG.Core.Types;

namespace AxiomRPG.ECS;

public class Entity
{
    public EntityId Id { get; }
    public string? TemplateId { get; init; }
    public string EntityType { get; init; } = "generic";

    public Entity(EntityId id)
    {
        Id = id;
    }
}
