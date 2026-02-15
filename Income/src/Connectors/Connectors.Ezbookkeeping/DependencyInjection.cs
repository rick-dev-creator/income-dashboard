using Connectors.Ezbookkeeping.Services;
using Income.Application.Connectors;
using Microsoft.Extensions.DependencyInjection;

namespace Connectors.Ezbookkeeping;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the ezbookkeeping connector and its dependencies.
    /// </summary>
    public static IServiceCollection AddEzbookkeepingConnector(this IServiceCollection services)
    {
        // Register HttpClient factory
        services.AddHttpClient();

        // Register internal services
        services.AddSingleton<EzbookkeepingApiClient>();

        // Register as ISyncableConnector for discovery by ConnectorRegistry
        services.AddSingleton<ISyncableConnector, EzbookkeepingConnector>();

        return services;
    }
}
