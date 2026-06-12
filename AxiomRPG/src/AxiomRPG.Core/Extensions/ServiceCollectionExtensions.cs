using AxiomRPG.Core.Events;
using AxiomRPG.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AxiomRPG.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAxiomCore(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, EventBus>();
        return services;
    }
}
