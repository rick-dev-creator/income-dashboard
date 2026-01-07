using Analytics.Application.Services;
using Analytics.Contracts.Queries;
using Analytics.Infrastructure.Handlers;
using Analytics.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Analytics.Installer;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        // Query handlers (internal)
        services.AddScoped<IGetPortfolioSummaryHandler, GetPortfolioSummaryHandler>();
        services.AddScoped<IGetIncomeTimeSeriesHandler, GetIncomeTimeSeriesHandler>();
        services.AddScoped<IGetDistributionHandler, GetDistributionHandler>();
        services.AddScoped<IGetPeriodComparisonHandler, GetPeriodComparisonHandler>();
        services.AddScoped<IGetProjectionHandler, GetProjectionHandler>();
        services.AddScoped<IGetTrendHandler, GetTrendHandler>();
        services.AddScoped<IGetTopPerformersHandler, GetTopPerformersHandler>();
        services.AddScoped<IGetDailyRateHandler, GetDailyRateHandler>();
        services.AddScoped<IGetStackedTimeSeriesHandler, GetStackedTimeSeriesHandler>();
        services.AddScoped<IGetStreamTrendsHandler, GetStreamTrendsHandler>();

        // Application Services (for Blazor/Frontend consumption)
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}
