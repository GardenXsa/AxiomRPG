using Microsoft.Extensions.DependencyInjection;

namespace AxiomRPG.World.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAxiomWorld(this IServiceCollection services)
    {
        services.AddSingleton<WorldManager>();
        return services;
    }
}
