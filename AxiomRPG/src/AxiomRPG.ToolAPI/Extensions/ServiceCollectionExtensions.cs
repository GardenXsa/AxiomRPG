using Microsoft.Extensions.DependencyInjection;

namespace AxiomRPG.ToolAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAxiomToolAPI(this IServiceCollection services)
    {
        ToolRegistry.RegisterAllTools(services);
        return services;
    }
}
