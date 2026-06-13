using AxiomRPG.ECS;
using AxiomRPG.World;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.Rendering.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAxiomRendering(this IServiceCollection services, int screenWidth = 80, int screenHeight = 50)
    {
        services.AddSingleton(sp => new ASCIIRenderer(
            sp.GetRequiredService<EntityManager>(),
            sp.GetRequiredService<WorldManager>(),
            sp.GetRequiredService<ILogger<ASCIIRenderer>>(),
            screenWidth,
            screenHeight
        ));
        return services;
    }
}
