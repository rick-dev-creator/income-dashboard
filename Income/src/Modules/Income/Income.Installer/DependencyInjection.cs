using Income.Application.Services;
using Income.Application.Services.Providers;
using Income.Application.Services.Streams;
using Income.Contracts.Queries;
using Income.Infrastructure.Features.Providers.Handlers;
using Income.Infrastructure.Features.Streams.Handlers;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Seeding;
using Income.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Income.Installer;

public static class DependencyInjection
{
    public static IServiceCollection AddIncomeModule(
        this IServiceCollection services,
        string connectionString,
        string encryptionKey = "FlowMetrics_Default_Key_Change_In_Production!")
    {
        // DbContext Factory (for Blazor Server thread safety)
        services.AddDbContextFactory<IncomeDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Core Services
        services.AddSingleton<ICredentialEncryptor>(new AesCredentialEncryptor(encryptionKey));

        // Seeding
        services.AddScoped<ISeedDataGenerator, SeedDataGenerator>();

        // Query Handlers (for cross-module communication via Contracts)
        services.AddScoped<IGetAllStreamsHandler, GetAllStreamsHandler>();
        services.AddScoped<IGetAllProvidersHandler, GetAllProvidersHandler>();

        // Application Services (for Blazor/Frontend consumption)
        services.AddScoped<IStreamService, StreamService>();
        services.AddScoped<IProviderService, ProviderService>();

        return services;
    }

    public static async Task InitializeIncomeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IncomeDbContext>>();
        await using var context = await factory.CreateDbContextAsync(ct);
        await context.Database.EnsureCreatedAsync(ct);
    }

    public static async Task SeedIncomeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ISeedDataGenerator>();
        await seeder.SeedAsync(ct);
    }
}
