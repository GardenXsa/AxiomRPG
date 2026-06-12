using AxiomRPG.Core.Interfaces;
using AxiomRPG.ECS;
using AxiomRPG.Simulation.Systems;
using AxiomRPG.World;
using Microsoft.Extensions.DependencyInjection;

namespace AxiomRPG.Simulation.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAxiomSimulation(this IServiceCollection services)
    {
        services.AddSingleton<GameTime>();
        services.AddSingleton<EntityManager>();
        services.AddSingleton<SimulationEngine>();
        services.AddSingleton<ISystem, MovementSystem>();
        services.AddSingleton<ISystem, CombatSystem>();
        services.AddSingleton<ISystem, AISystem>();
        services.AddSingleton<ISystem, WeatherSystem>();
        return services;
    }
}
