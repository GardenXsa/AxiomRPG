namespace AxiomRPG.Core.Interfaces;

public interface IEventBus
{
    void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
    void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
    Task PublishAsync<TEvent>(TEvent eventData) where TEvent : IEvent;
}
