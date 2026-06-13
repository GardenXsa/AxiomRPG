using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AxiomRPG.AI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAxiomAI(this IServiceCollection services, AIClientConfiguration? config = null)
    {
        services.AddSingleton(config ?? new AIClientConfiguration());
        services.AddSingleton<ILLMClient>(sp =>
        {
            var cfg = sp.GetRequiredService<AIClientConfiguration>();
            var logger = sp.GetRequiredService<ILogger<OpenAIClient>>();
            return new OpenAIClient(cfg, logger);
        });
        services.AddSingleton<AgentOrchestrator>();
        return services;
    }
}
