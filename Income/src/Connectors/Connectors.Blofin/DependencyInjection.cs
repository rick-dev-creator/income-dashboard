using Connectors.Blofin.Services;
using Income.Application.Connectors;
using Microsoft.Extensions.DependencyInjection;

namespace Connectors.Blofin;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Blofin connector and its dependencies.
    /// </summary>
    public static IServiceCollection AddBlofinConnector(this IServiceCollection services)
    {
        // Register HttpClient factory
        services.AddHttpClient();

        // Register internal services
        services.AddSingleton<BlofinApiClient>();

        // Register as ISyncableConnector for discovery by ConnectorRegistry
        services.AddSingleton<ISyncableConnector, BlofinConnector>();

        return services;
    }
}
