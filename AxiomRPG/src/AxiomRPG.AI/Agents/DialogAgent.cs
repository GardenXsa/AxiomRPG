using AxiomRPG.Core.Interfaces;
using AxiomRPG.ToolAPI;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.AI.Agents;

public class DialogAgent : AgentBase
{
    public override string AgentType => "dialog";

    private static readonly string DefaultSystemPrompt = """
        You are the Dialog AI for an ASCII RPG game. You handle all NPC conversations.

        You have tools to:
        - Read NPC personality and dialog data
        - Check and modify NPC relations with the player
        - Set dialog flags that affect quest availability
        - Read the player's current state

        Rules:
        1. Stay in character — each NPC has a unique personality
        2. Dialog should feel natural and reactive
        3. Player choices should matter — set appropriate dialog flags
        4. NPC reactions should reflect their relationship with the player
        5. Use tools to READ before responding, WRITE to update game state
        """;

    public DialogAgent(
        string agentId,
        ILLMClient llmClient,
        ToolDispatcher toolDispatcher,
        IEventBus eventBus,
        ILogger<DialogAgent> logger
    ) : base(agentId, llmClient, toolDispatcher, eventBus, logger, DefaultSystemPrompt)
    {
    }
}
