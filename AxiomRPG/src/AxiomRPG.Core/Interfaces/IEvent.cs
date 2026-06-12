namespace AxiomRPG.Core.Interfaces;

public interface IEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
}
