using AxiomRPG.Core.Interfaces;
using AxiomRPG.Data;
using AxiomRPG.ECS;
using AxiomRPG.ToolAPI.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace AxiomRPG.ToolAPI;

public static class ToolRegistry
{
    public static void RegisterAllTools(IServiceCollection services)
    {
        services.AddSingleton<ToolDispatcher>();

        // World Builder tools
        services.AddSingleton<IToolHandler, CreateBiomeTool>();
        services.AddSingleton<IToolHandler, CreateCreatureTemplateTool>();
        services.AddSingleton<IToolHandler, CreateItemTool>();
        services.AddSingleton<IToolHandler, CreateLocationTool>();
        services.AddSingleton<IToolHandler, CreateFactionTool>();
        services.AddSingleton<IToolHandler, CreateStatSystemTool>();
        services.AddSingleton<IToolHandler, FinalizeWorldTool>();

        // Game Master tools
        services.AddSingleton<IToolHandler, ReadRegionTool>();
        services.AddSingleton<IToolHandler, SpawnEntityTool>();
        services.AddSingleton<IToolHandler, CreateQuestTool>();
        services.AddSingleton<IToolHandler, GiveItemTool>();
    }

    public static ToolDispatcher BuildDispatcher(IServiceProvider serviceProvider)
    {
        var dispatcher = serviceProvider.GetRequiredService<ToolDispatcher>();
        var handlers = serviceProvider.GetServices<IToolHandler>();
        foreach (var handler in handlers)
        {
            dispatcher.RegisterHandler(handler);
        }
        return dispatcher;
    }
}
