namespace AxiomRPG.Core.Events;

public record WorldCreatedEvent(string PlanetId, string WorldName) : GameEvent;
public record ChunkLoadedEvent(string PlanetId, int ChunkX, int ChunkY) : GameEvent;
public record EntitySpawnedEvent(string EntityId, string TemplateId, int X, int Y) : GameEvent;
public record EntityMovedEvent(string EntityId, int FromX, int FromY, int ToX, int ToY) : GameEvent;
public record EntityDiedEvent(string EntityId, string? KilledBy) : GameEvent;
public record PlayerCreatedEvent(string PlayerId, string Name) : GameEvent;
public record QuestCreatedEvent(string QuestId, string Title, string AssignedTo) : GameEvent;
public record QuestCompletedEvent(string QuestId, string CompletedBy) : GameEvent;
public record ItemGivenEvent(string ItemId, string GivenTo, string? GivenBy) : GameEvent;
public record WeatherChangedEvent(string RegionId, string NewWeather) : GameEvent;
public record FactionRelationChangedEvent(string FactionA, string FactionB, int NewRelation) : GameEvent;
public record DialogStartedEvent(string NpcId, string PlayerId) : GameEvent;
public record CombatStartedEvent(string AttackerId, string DefenderId) : GameEvent;
