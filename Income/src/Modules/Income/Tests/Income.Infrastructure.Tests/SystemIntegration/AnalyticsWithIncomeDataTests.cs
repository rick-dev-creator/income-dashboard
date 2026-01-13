using Analytics.Contracts.Queries;
using Analytics.Infrastructure.Handlers;
using Income.Application.Connectors;
using Income.Contracts.Queries;
using Income.Infrastructure.Features.Streams.Handlers;
using Income.Infrastructure.Features.Providers.Handlers;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Seeding;
using Income.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Income.Infrastructure.Tests.SystemIntegration;

[Collection("Postgres")]
public class AnalyticsWithIncomeDataTests(PostgresFixture fixture) : IAsyncLifetime
{
    private IDbContextFactory<IncomeDbContext> _factory = null!;
    private IGetAllStreamsHandler _streamsHandler = null!;
    private IGetAllProvidersHandler _providersHandler = null!;
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private static bool _isSeeded;

    public async Task InitializeAsync()
    {
        _factory = fixture.CreateFactory();
        _streamsHandler = new GetAllStreamsHandler(_factory);
        _providersHandler = new GetAllProvidersHandler(_factory);

        await SeedDatabaseOnceAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task SeedDatabaseOnceAsync()
    {
        if (_isSeeded) return;

        await SeedLock.WaitAsync();
        try
        {
            if (_isSeeded) return;

            // Always seed our specific test data (using unique IDs to avoid conflicts)
            var seeder = new SeedDataGenerator(_factory, new EmptyConnectorRegistry(), NullLogger<SeedDataGenerator>.Instance);
            await seeder.SeedAsync(); // Will skip if our specific providers already exist
            _isSeeded = true;
        }
        finally
        {
            SeedLock.Release();
        }
    }

    [Fact]
    public async Task PortfolioSummary_WithSeededData_ReturnsValidSummary()
    {
        // Arrange
        var portfolioHandler = new GetPortfolioSummaryHandler(_streamsHandler, _providersHandler);

        // Act
        var result = await portfolioHandler.HandleAsync(new GetPortfolioSummaryQuery());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.StreamCount.ShouldBeGreaterThanOrEqualTo(5);
        result.Value.ProviderCount.ShouldBeGreaterThanOrEqualTo(3);
        result.Value.TotalIncomeUsd.ShouldBeGreaterThan(0);
        result.Value.ActiveStreamCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task IncomeTimeSeries_WithSeededData_ReturnsTimeSeriesPoints()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var timeSeriesHandler = new GetIncomeTimeSeriesHandler(_streamsHandler);

        // Act
        var result = await timeSeriesHandler.HandleAsync(new GetIncomeTimeSeriesQuery(
            StartDate: DateOnly.FromDateTime(now.AddMonths(-6)),
            EndDate: DateOnly.FromDateTime(now),
            Granularity: "Monthly"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Points.Count.ShouldBeGreaterThan(0);
        result.Value.TotalUsd.ShouldBeGreaterThan(0);
        result.Value.Granularity.ShouldBe("Monthly");
    }

    [Fact]
    public async Task Distribution_ByCategory_ReturnsCorrectBreakdown()
    {
        // Arrange
        var distributionHandler = new GetDistributionHandler(_streamsHandler, _providersHandler);

        // Act
        var result = await distributionHandler.HandleAsync(new GetDistributionQuery(GroupBy: "Category"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBeGreaterThan(0);

        // Should have salary, trading, subscription, referral categories
        var categories = result.Value.Items.Select(i => i.Key).ToList();
        categories.ShouldContain("Salary");
        categories.ShouldContain("Trading");
        categories.ShouldContain("Subscription");

        // Percentages should sum to ~100%
        var totalPercentage = result.Value.Items.Sum(i => i.Percentage);
        totalPercentage.ShouldBe(100, 0.1m);
    }

    [Fact]
    public async Task Distribution_ByProvider_ReturnsCorrectBreakdown()
    {
        // Arrange
        var distributionHandler = new GetDistributionHandler(_streamsHandler, _providersHandler);

        // Act
        var result = await distributionHandler.HandleAsync(new GetDistributionQuery(GroupBy: "Provider"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBeGreaterThanOrEqualTo(3);

        var providerNames = result.Value.Items.Select(i => i.Label).ToList();
        providerNames.ShouldContain("[Seed] Manual Entry");
        providerNames.ShouldContain("[Seed] Blofin");
        providerNames.ShouldContain("[Seed] Patreon");
    }

    [Fact]
    public async Task PeriodComparison_MonthOverMonth_ReturnsComparison()
    {
        // Arrange
        var comparisonHandler = new GetPeriodComparisonHandler(_streamsHandler);

        // Act
        var result = await comparisonHandler.HandleAsync(new GetPeriodComparisonQuery(
            ComparisonType: "MoM"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrentPeriod.ShouldNotBeNull();
        result.Value.PreviousPeriod.ShouldNotBeNull();
        result.Value.Trend.ShouldBeOneOf("Up", "Down", "Flat");
    }

    [Fact]
    public async Task Projection_Returns12MonthForecast()
    {
        // Arrange
        var projectionHandler = new GetProjectionHandler(_streamsHandler);

        // Act
        var result = await projectionHandler.HandleAsync(new GetProjectionQuery(MonthsAhead: 12));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.MonthlyProjections.Count.ShouldBe(12);
        result.Value.ProjectedMonthlyIncomeUsd.ShouldBeGreaterThanOrEqualTo(0);
        result.Value.ProjectedAnnualIncomeUsd.ShouldBe(result.Value.ProjectedMonthlyIncomeUsd * 12, 0.1m);
        result.Value.ConfidenceScore.ShouldBeInRange(0, 1);
    }

    [Fact]
    public async Task Trend_Monthly_ReturnsGrowthData()
    {
        // Arrange
        var trendHandler = new GetTrendHandler(_streamsHandler);

        // Act
        var result = await trendHandler.HandleAsync(new GetTrendQuery(
            Period: "Monthly",
            PeriodsBack: 6));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Period.ShouldBe("Monthly");
        result.Value.Direction.ShouldBeOneOf("Upward", "Downward", "Stable");
        result.Value.Points.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task TopPerformers_ReturnsRankedStreams()
    {
        // Arrange
        var topPerformersHandler = new GetTopPerformersHandler(_streamsHandler, _providersHandler);

        // Act
        var result = await topPerformersHandler.HandleAsync(new GetTopPerformersQuery(TopN: 5));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBeGreaterThan(0);
        result.Value.Items.Count.ShouldBeLessThanOrEqualTo(5);

        // Verify ranking is in order
        for (var i = 1; i < result.Value.Items.Count; i++)
        {
            result.Value.Items[i].TotalUsd.ShouldBeLessThanOrEqualTo(result.Value.Items[i - 1].TotalUsd);
            result.Value.Items[i].Rank.ShouldBe(i + 1);
        }

        // Verify we have salary category streams (from seed data)
        var salaryStream = result.Value.Items.FirstOrDefault(i => i.Category == "Salary");
        salaryStream.ShouldNotBeNull();
    }

    [Fact]
    public async Task IncomeTimeSeries_FilterByCategory_ReturnsFilteredData()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var timeSeriesHandler = new GetIncomeTimeSeriesHandler(_streamsHandler);

        // Act
        var result = await timeSeriesHandler.HandleAsync(new GetIncomeTimeSeriesQuery(
            StartDate: DateOnly.FromDateTime(now.AddMonths(-6)),
            EndDate: DateOnly.FromDateTime(now),
            Granularity: "Monthly",
            Category: "Salary"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Points.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task FullPipeline_SeedToAnalytics_WorksEndToEnd()
    {
        // Step 1: Verify Income data is available
        var streamsResult = await _streamsHandler.HandleAsync(new GetAllStreamsQuery());
        streamsResult.IsSuccess.ShouldBeTrue();
        streamsResult.Value.Count.ShouldBeGreaterThanOrEqualTo(5);

        var providersResult = await _providersHandler.HandleAsync(new GetAllProvidersQuery());
        providersResult.IsSuccess.ShouldBeTrue();
        providersResult.Value.Count.ShouldBeGreaterThanOrEqualTo(3);

        // Step 2: Run all analytics and verify they work
        var portfolioHandler = new GetPortfolioSummaryHandler(_streamsHandler, _providersHandler);
        var distributionHandler = new GetDistributionHandler(_streamsHandler, _providersHandler);
        var projectionHandler = new GetProjectionHandler(_streamsHandler);
        var topPerformersHandler = new GetTopPerformersHandler(_streamsHandler, _providersHandler);

        var portfolio = await portfolioHandler.HandleAsync(new GetPortfolioSummaryQuery());
        var distribution = await distributionHandler.HandleAsync(new GetDistributionQuery(GroupBy: "Category"));
        var projection = await projectionHandler.HandleAsync(new GetProjectionQuery(MonthsAhead: 6));
        var topPerformers = await topPerformersHandler.HandleAsync(new GetTopPerformersQuery(TopN: 3));

        // All should succeed
        portfolio.IsSuccess.ShouldBeTrue();
        distribution.IsSuccess.ShouldBeTrue();
        projection.IsSuccess.ShouldBeTrue();
        topPerformers.IsSuccess.ShouldBeTrue();

        // Cross-validate data consistency
        var totalFromDistribution = distribution.Value.TotalUsd;
        portfolio.Value.TotalIncomeUsd.ShouldBe(totalFromDistribution, 0.01m);
    }

    /// <summary>
    /// Empty connector registry for testing - returns no connectors.
    /// </summary>
    private sealed class EmptyConnectorRegistry : IConnectorRegistry
    {
        public IReadOnlyList<IIncomeConnector> GetAll() => [];
        public IReadOnlyList<ISyncableConnector> GetSyncable() => [];
        public IReadOnlyList<IRecurringConnector> GetRecurring() => [];
        public IIncomeConnector? GetById(string providerId) => null;
        public ISyncableConnector? GetSyncableById(string providerId) => null;
        public IRecurringConnector? GetRecurringById(string providerId) => null;
    }
}
