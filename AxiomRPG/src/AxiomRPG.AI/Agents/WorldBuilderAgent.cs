using System.Runtime.CompilerServices;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.ToolAPI;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.AI.Agents;

public class WorldBuilderAgent : AgentBase
{
    public override string AgentType => "world_builder";

    private static readonly string DefaultSystemPrompt = """
        You are the World Builder AI for an ASCII RPG game. Your job is to create a complete, rich, and detailed game world based on the player's description.

        You have tools to create:
        - Stat systems (define stats, custom indicators, derived stats)
        - Biomes (terrain types with flora, fauna, weather)
        - Creature templates (NPCs, monsters, animals with stats and behavior)
        - Items (weapons, armor, consumables, quest items)
        - Locations (cities, villages, dungeons, caves with internal structure)
        - Factions (groups with inter-faction relations)

        RULES:
        1. Create the stat system FIRST — all creatures and the player use the same stat system
        2. Create diverse biomes — at least 5-6 different biome types
        3. Create creature templates for each biome — predators, prey, and humanoids
        4. Create meaningful locations — cities should have districts, villages should have character
        5. Create factions that create conflict and alliance opportunities
        6. Be detailed and creative — this world needs to feel alive
        7. When finished, call finalize_world to signal completion
        8. Each tool call creates data that the game engine will load

        Remember: A city is NOT a point on a map. It spans multiple chunks with districts, buildings, and NPCs.
        """;

    public WorldBuilderAgent(
        string agentId,
        ILLMClient llmClient,
        ToolDispatcher toolDispatcher,
        IEventBus eventBus,
        ILogger<WorldBuilderAgent> logger
    ) : base(agentId, llmClient, toolDispatcher, eventBus, logger, DefaultSystemPrompt,
        toolDispatcher.GetAllToolDefinitions().Where(t =>
            t.Name.StartsWith("create_") || t.Name == "finalize_world").ToList())
    {
    }

    public async IAsyncEnumerable<string> BuildWorldAsync(
        string worldDescription,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        await StartAsync(ct);

        await foreach (var chunk in StreamResponseAsync(worldDescription, ct))
        {
            yield return chunk;
        }
    }
}
