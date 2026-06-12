using System.Text.Json.Nodes;

namespace AxiomRPG.Core.Interfaces;

public interface IAgent
{
    string AgentId { get; }
    string AgentType { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task<AgentResponse> SendMessageAsync(AgentMessage message, CancellationToken ct = default);
}

public record AgentMessage(string Role, string Content, JsonNode? ToolCall = null);
public record AgentResponse(string Content, bool RequiresToolExecution, JsonNode? ToolCallResult = null);
