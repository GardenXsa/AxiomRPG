using AxiomRPG.Core.Types;

namespace AxiomRPG.ECS;

public class EntityManager
{
    private readonly Dictionary<EntityId, Entity> _entities = new();
    private readonly Dictionary<Type, Dictionary<EntityId, IComponent>> _componentsByType = new();
    private readonly object _lock = new();

    public Entity CreateEntity(EntityId? id = null)
    {
        var entityId = id ?? EntityId.New();
        var entity = new Entity(entityId);
        lock (_lock)
        {
            _entities[entityId] = entity;
        }
        return entity;
    }

    public Entity? GetEntity(EntityId id)
    {
        lock (_lock)
        {
            return _entities.GetValueOrDefault(id);
        }
    }

    public void DestroyEntity(EntityId id)
    {
        lock (_lock)
        {
            if (_entities.Remove(id))
            {
                foreach (var dict in _componentsByType.Values)
                {
                    dict.Remove(id);
                }
            }
        }
    }

    public void AddComponent<T>(EntityId id, T component) where T : IComponent
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (!_componentsByType.TryGetValue(type, out var dict))
            {
                dict = new Dictionary<EntityId, IComponent>();
                _componentsByType[type] = dict;
            }
            dict[id] = component;
        }
    }

    public T? GetComponent<T>(EntityId id) where T : IComponent
    {
        lock (_lock)
        {
            if (_componentsByType.TryGetValue(typeof(T), out var dict) && dict.TryGetValue(id, out var comp))
            {
                return (T)comp;
            }
        }
        return default;
    }

    public bool HasComponent<T>(EntityId id) where T : IComponent
    {
        lock (_lock)
        {
            return _componentsByType.TryGetValue(typeof(T), out var dict) && dict.ContainsKey(id);
        }
    }

    public void RemoveComponent<T>(EntityId id) where T : IComponent
    {
        lock (_lock)
        {
            if (_componentsByType.TryGetValue(typeof(T), out var dict))
            {
                dict.Remove(id);
            }
        }
    }

    public IReadOnlyList<Entity> QueryEntitiesWith<T1>() where T1 : IComponent
    {
        lock (_lock)
        {
            if (!_componentsByType.TryGetValue(typeof(T1), out var dict))
                return Array.Empty<Entity>();

            return dict.Keys
                .Select(id => _entities.GetValueOrDefault(id))
                .Where(e => e != null)
                .Cast<Entity>()
                .ToList();
        }
    }

    public IReadOnlyList<Entity> QueryEntitiesWith<T1, T2>() where T1 : IComponent where T2 : IComponent
    {
        lock (_lock)
        {
            if (!_componentsByType.TryGetValue(typeof(T1), out var dict1) ||
                !_componentsByType.TryGetValue(typeof(T2), out var dict2))
                return Array.Empty<Entity>();

            var commonIds = dict1.Keys.Intersect(dict2.Keys);
            return commonIds
                .Select(id => _entities.GetValueOrDefault(id))
                .Where(e => e != null)
                .Cast<Entity>()
                .ToList();
        }
    }

    public IReadOnlyList<Entity> GetAllEntities()
    {
        lock (_lock)
        {
            return _entities.Values.ToList();
        }
    }

    public IEnumerable<EntityId> GetAllEntityIds()
    {
        lock (_lock)
        {
            return _entities.Keys.ToList();
        }
    }
}
