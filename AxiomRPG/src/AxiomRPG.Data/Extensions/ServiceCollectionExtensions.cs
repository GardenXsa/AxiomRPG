using Microsoft.Extensions.DependencyInjection;

namespace AxiomRPG.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAxiomData(this IServiceCollection services, string dataPath)
    {
        services.AddSingleton<IDataStore>(sp => new FileSystemDataStore(dataPath));
        services.AddSingleton<DataService>();
        return services;
    }
}
