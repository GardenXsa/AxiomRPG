using AxiomRPG.Core.Events;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.Core.Types;
using AxiomRPG.ECS;
using AxiomRPG.Components;
using AxiomRPG.World;

namespace AxiomRPG.Simulation.Systems;

public class MovementSystem : ISystem
{
    public int Priority => 10;

    private readonly EntityManager _entityManager;
    private readonly WorldManager _worldManager;
    private readonly IEventBus _eventBus;

    public MovementSystem(EntityManager entityManager, WorldManager worldManager, IEventBus eventBus)
    {
        _entityManager = entityManager;
        _worldManager = worldManager;
        _eventBus = eventBus;
    }

    public void Initialize() { }

    public void Update(float deltaTime)
    {
        // Process entities with position that need to move
        var entities = _entityManager.QueryEntitiesWith<PositionComponent>();
        // Movement logic handled via MoveEntity calls, not automatic
    }

    public async Task<bool> MoveEntityAsync(string entityId, int dx, int dy)
    {
        var entity = _entityManager.GetEntity(EntityId.From(entityId));
        if (entity == null) return false;

        var pos = _entityManager.GetComponent<PositionComponent>(entity.Id);
        if (pos == null) return false;

        var newX = pos.X + dx;
        var newY = pos.Y + dy;

        // Check tile blocking
        var chunkX = newX / Chunk.ChunkSize;
        var chunkY = newY / Chunk.ChunkSize;
        var localX = ((newX % Chunk.ChunkSize) + Chunk.ChunkSize) % Chunk.ChunkSize;
        var localY = ((newY % Chunk.ChunkSize) + Chunk.ChunkSize) % Chunk.ChunkSize;

        var chunk = await _worldManager.GetOrLoadChunkAsync(chunkX, chunkY);
        var tile = chunk.GetTile(localX, localY);

        if (tile.IsBlocking) return false;

        var oldX = pos.X;
        var oldY = pos.Y;

        // Update position component
        _entityManager.AddComponent(entity.Id, pos with { X = newX, Y = newY });

        await _eventBus.PublishAsync(new EntityMovedEvent(entityId, oldX, oldY, newX, newY));
        return true;
    }
}
