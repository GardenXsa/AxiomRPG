using System.Collections.Concurrent;
using AxiomRPG.Core.Interfaces;

namespace AxiomRPG.Core.Events;

public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new List<object>());
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }

    public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent eventData) where TEvent : IEvent
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;

        List<object> snapshot;
        lock (handlers)
        {
            snapshot = handlers.ToList();
        }

        foreach (var handler in snapshot)
        {
            if (handler is IEventHandler<TEvent> typedHandler)
            {
                await typedHandler.HandleAsync(eventData);
            }
        }
    }
}
