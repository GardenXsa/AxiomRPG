using Microsoft.Extensions.Logging;

namespace AxiomRPG.ToolAPI;

public class ToolDispatcher
{
    private readonly Dictionary<string, IToolHandler> _handlers = new();
    private readonly ILogger<ToolDispatcher> _logger;

    public ToolDispatcher(ILogger<ToolDispatcher> logger)
    {
        _logger = logger;
    }

    public void RegisterHandler(IToolHandler handler)
    {
        _handlers[handler.ToolName] = handler;
        _logger.LogInformation("Registered tool: {Name}", handler.ToolName);
    }

    public async Task<ToolResult> DispatchAsync(ToolCall call)
    {
        if (!_handlers.TryGetValue(call.ToolName, out var handler))
        {
            _logger.LogWarning("Unknown tool called: {Name}", call.ToolName);
            return ToolResult.Error(call.CallId, $"Unknown tool: {call.ToolName}");
        }

        try
        {
            return await handler.ExecuteAsync(call);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {Name} execution failed", call.ToolName);
            return ToolResult.Error(call.CallId, $"Tool execution error: {ex.Message}");
        }
    }

    public IReadOnlyList<ToolDefinition> GetAllToolDefinitions() =>
        _handlers.Values.Select(h => h.GetDefinition()).ToList();

    public bool HasTool(string name) => _handlers.ContainsKey(name);
}
