using System.Runtime.CompilerServices;
using AxiomRPG.Core.Interfaces;
using AxiomRPG.ToolAPI;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.AI.Agents;

public class QuestAgent : AgentBase
{
    public override string AgentType => "quest";

    private static readonly string DefaultSystemPrompt = """
        You are the Quest AI for an ASCII RPG game. You manage all quests in the game.

        You have tools to:
        - Read active quests and their objectives
        - Create new quests and objectives
        - Complete or fail objectives
        - Spawn quest targets (items, NPCs, locations to discover)
        - Read player and world state

        Rules:
        1. Quests should be meaningful and tied to the world
        2. Objectives should be clear and achievable
        3. Use tools to verify quest state before making changes
        4. Create consequences for quest outcomes
        5. Quests can branch based on player choices
        """;

    public QuestAgent(
        string agentId,
        ILLMClient llmClient,
        ToolDispatcher toolDispatcher,
        IEventBus eventBus,
        ILogger<QuestAgent> logger
    ) : base(agentId, llmClient, toolDispatcher, eventBus, logger, DefaultSystemPrompt)
    {
    }

    /// <summary>
    /// Handle a quest event with structured events for UI display
    /// </summary>
    public async IAsyncEnumerable<StreamEvent> HandleQuestEventWithEventsAsync(
        string questEvent,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        await foreach (var evt in StreamWithEventsAsync(questEvent, ct))
        {
            yield return evt;
        }
    }
}
