using AxiomRPG.Core.Interfaces;

namespace AxiomRPG.Core.Events;

public abstract record GameEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
