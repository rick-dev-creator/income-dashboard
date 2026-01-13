using Connectors.Toobit.Services;
using Income.Application.Connectors;
using Microsoft.Extensions.DependencyInjection;

namespace Connectors.Toobit;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Toobit connector and its dependencies.
    /// </summary>
    public static IServiceCollection AddToobitConnector(this IServiceCollection services)
    {
        // Register HttpClient factory (if not already registered)
        services.AddHttpClient();

        // Register internal services
        services.AddSingleton<ToobitApiClient>();

        // Register as ISyncableConnector for discovery by ConnectorRegistry
        services.AddSingleton<ISyncableConnector, ToobitConnector>();

        return services;
    }
}
