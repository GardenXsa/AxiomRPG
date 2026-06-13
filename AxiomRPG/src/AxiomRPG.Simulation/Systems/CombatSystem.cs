using AxiomRPG.Core.Events;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.Core.Types;
using AxiomRPG.ECS;
using AxiomRPG.Components;

namespace AxiomRPG.Simulation.Systems;

public class CombatSystem : ISystem
{
    public int Priority => 20;

    private readonly EntityManager _entityManager;
    private readonly IEventBus _eventBus;
    private readonly Random _random = new();

    public CombatSystem(EntityManager entityManager, IEventBus eventBus)
    {
        _entityManager = entityManager;
        _eventBus = eventBus;
    }

    public void Initialize() { }

    public void Update(float deltaTime)
    {
        // Check for combat triggers - entities in range with hostile factions
    }

    public async Task<CombatResult> AttackAsync(string attackerId, string defenderId)
    {
        var attacker = _entityManager.GetEntity(EntityId.From(attackerId));
        var defender = _entityManager.GetEntity(EntityId.From(defenderId));

        if (attacker == null || defender == null)
            return new CombatResult(false, 0, "Invalid entities");

        var attackerStats = _entityManager.GetComponent<StatsComponent>(attacker.Id);
        var defenderStats = _entityManager.GetComponent<StatsComponent>(defender.Id);
        var defenderHealth = _entityManager.GetComponent<HealthComponent>(defender.Id);

        if (attackerStats == null || defenderStats == null || defenderHealth == null)
            return new CombatResult(false, 0, "Missing components");

        // Calculate damage
        var attackPower = attackerStats.GetStat("str") + _random.Next(1, 7);
        var defense = defenderStats.GetStat("con") / 2;
        var damage = Math.Max(1, (int)(attackPower - defense));

        defenderHealth.CurrentHp -= damage;

        var killed = defenderHealth.IsDead;
        if (killed)
        {
            await _eventBus.PublishAsync(new EntityDiedEvent(defenderId, attackerId));
        }

        await _eventBus.PublishAsync(new CombatStartedEvent(attackerId, defenderId));

        return new CombatResult(true, damage, killed ? "Target killed" : $"Dealt {damage} damage");
    }
}

public record CombatResult(bool Success, int Damage, string Message);
