namespace AxiomRPG.Core.Interfaces;

public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent eventData);
}
