using AxiomRPG.Core.Types;
using AxiomRPG.ECS;
using AxiomRPG.Components;

namespace AxiomRPG.Simulation.Systems;

public class AISystem : ISystem
{
    public int Priority => 30;

    private readonly EntityManager _entityManager;
    private readonly Random _random = new();

    public AISystem(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public void Initialize() { }

    public void Update(float deltaTime)
    {
        var aiEntities = _entityManager.QueryEntitiesWith<AIComponent, PositionComponent>();

        foreach (var entity in aiEntities)
        {
            var ai = _entityManager.GetComponent<AIComponent>(entity.Id);
            var pos = _entityManager.GetComponent<PositionComponent>(entity.Id);
            if (ai == null || pos == null) continue;

            switch (ai.BehaviorType)
            {
                case "wander":
                    ProcessWander(entity.Id.Value, pos);
                    break;
                case "idle":
                    // Do nothing
                    break;
                case "patrol":
                    ProcessPatrol(entity.Id.Value, pos, ai);
                    break;
                // Other behaviors handled by AI agents
            }
        }
    }

    private void ProcessWander(string entityId, PositionComponent pos)
    {
        if (_random.Next(100) < 5) // 5% chance to move each update
        {
            var dx = _random.Next(-1, 2);
            var dy = _random.Next(-1, 2);
            if (dx != 0 || dy != 0)
            {
                _entityManager.AddComponent(EntityId.From(entityId), pos with { X = pos.X + dx, Y = pos.Y + dy });
            }
        }
    }

    private void ProcessPatrol(string entityId, PositionComponent pos, AIComponent ai)
    {
        // Simple patrol — move back and forth
        if (_random.Next(100) < 3)
        {
            var dx = _random.Next(0, 2) == 0 ? 1 : -1;
            _entityManager.AddComponent(EntityId.From(entityId), pos with { X = pos.X + dx });
        }
    }
}
