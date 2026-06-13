using System.Runtime.CompilerServices;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.ToolAPI;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.AI.Agents;

public class GameMasterAgent : AgentBase
{
    public override string AgentType => "game_master";

    private static readonly string DefaultSystemPrompt = """
        You are the Game Master AI for an ASCII RPG game. You are like a CLI for the game world — you READ data using tools, then make decisions and ACT using tools.

        IMPORTANT: You do NOT receive all world data at once. You must USE TOOLS to read the data you need:
        - Use read_region to understand where the player is
        - Use tools to check entity states, faction relations, etc.

        Your responsibilities:
        1. Choose spawn point for new players — find a suitable location by READING region data
        2. Give starting items and equipment
        3. Create initial quests appropriate to the player's location
        4. Spawn nearby creatures when needed (wolves in forests, bandits on roads, etc.)
        5. React to player actions by modifying the world
        6. Drive narrative through events and consequences
        7. Manage NPC behavior and world events

        You are NOT a narrator who describes things. You are an ACTIVE participant who uses tools to shape the game world in real-time.
        Always read data first, then act. Never assume — verify through tools.
        """;

    public GameMasterAgent(
        string agentId,
        ILLMClient llmClient,
        ToolDispatcher toolDispatcher,
        IEventBus eventBus,
        ILogger<GameMasterAgent> logger
    ) : base(agentId, llmClient, toolDispatcher, eventBus, logger, DefaultSystemPrompt)
    {
    }

    /// <summary>
    /// Initialize a new player using structured events for UI display.
    /// </summary>
    public async IAsyncEnumerable<StreamEvent> InitializePlayerWithEventsAsync(
        string playerDescription,
        string playerId,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var message = $"""
            A new player has entered the world. Here is their character description:

            Player ID: {playerId}
            Description: {playerDescription}

            Please:
            1. READ the region data to find a suitable spawn point
            2. SPAWN the player at an appropriate location
            3. GIVE them starting items appropriate to their character
            4. CREATE an initial quest for them
            5. SPAWN any nearby creatures that make sense for the location
            """;

        await foreach (var evt in StreamWithEventsAsync(message, ct))
        {
            yield return evt;
        }
    }

    /// <summary>
    /// Legacy text-only streaming for backward compatibility
    /// </summary>
    public async IAsyncEnumerable<string> InitializePlayerAsync(
        string playerDescription,
        string playerId,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        await foreach (var evt in InitializePlayerWithEventsAsync(playerDescription, playerId, ct))
        {
            if (evt.Type == StreamEventType.TextDelta)
                yield return evt.Content;
        }
    }
}
