using FluentResults;
using Income.Application.Connectors;
using Income.Application.Services.CsvImport;
using Income.Application.Services.Streams;
using Income.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Services;

internal sealed class CsvImportService(
    IDbContextFactory<IncomeDbContext> dbContextFactory,
    IConnectorRegistry connectorRegistry,
    IStreamService streamService) : ICsvImportService
{
    public async Task<Result<StreamExportPackage>> ExportStreamsForClassificationAsync(
        string providerId,
        CancellationToken ct = default)
    {
        var connector = connectorRegistry.GetCsvImportById(providerId);
        if (connector is null)
            return Result.Fail<StreamExportPackage>($"CSV import connector '{providerId}' not found");

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var streams = await dbContext.Streams
            .AsNoTracking()
            .Select(s => new StreamExportItem(
                s.Id,
                s.Name,
                s.Category,
                s.StreamType == 0 ? "Income" : "Outcome",
                null)) // TODO: Add keywords field to stream entity if needed
            .ToListAsync(ct);

        var instructions = connector.GetClassificationInstructions(streams);
        var outputFormat = connector.GetClassifiedCsvHeader();

        var bankFormatInfo = $"""
            Bank: {connector.BankName}
            Expected CSV Format:
            - Date Column: {connector.FormatSpec.DateColumn}
            - Date Format: {connector.FormatSpec.DateFormat}
            - Description Column: {connector.FormatSpec.DescriptionColumn}
            - Encoding: {connector.FormatSpec.Encoding}
            - Delimiter: {connector.FormatSpec.Delimiter}
            """;

        return new StreamExportPackage(streams, instructions, outputFormat, bankFormatInfo);
    }

    public async Task<Result<CsvImportResult>> ImportClassifiedTransactionsAsync(
        string providerId,
        IReadOnlyList<ClassifiedTransaction> transactions,
        IReadOnlyList<NewStreamRequest> newStreams,
        ImportAggregationMode aggregationMode,
        CancellationToken ct = default)
    {
        var connector = connectorRegistry.GetCsvImportById(providerId);
        if (connector is null)
            return Result.Fail<CsvImportResult>($"CSV import connector '{providerId}' not found");

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var warnings = new List<string>();
        var streamIdMap = new Dictionary<string, string>(); // SuggestedName -> ActualStreamId

        // 1. Create new streams first (amounts are already converted to USD by Claude)
        var newStreamsCreated = 0;
        foreach (var newStream in newStreams)
        {
            var createResult = await streamService.CreateAsync(new CreateStreamRequest(
                ProviderId: providerId,
                Name: newStream.Name,
                Category: newStream.Category,
                OriginalCurrency: "USD",
                IsFixed: false,
                FixedPeriod: null,
                StreamType: newStream.StreamType), ct);

            if (createResult.IsFailed)
            {
                warnings.Add($"Failed to create stream '{newStream.Name}': {createResult.Errors.First().Message}");
                continue;
            }

            streamIdMap[newStream.Name] = createResult.Value.Id;
            newStreamsCreated++;
        }

        // 2. Group and aggregate transactions based on mode
        var aggregatedSnapshots = AggregateTransactions(transactions, aggregationMode, streamIdMap);

        // 3. Create snapshots
        var snapshotsCreated = 0;
        foreach (var snapshot in aggregatedSnapshots)
        {
            if (string.IsNullOrEmpty(snapshot.StreamId))
            {
                warnings.Add($"Transaction on {snapshot.Date} '{snapshot.Description}' has no stream assigned");
                continue;
            }

            // Amount is already converted to USD by Claude Code during classification
            var recordResult = await streamService.RecordSnapshotAsync(new RecordSnapshotRequest(
                StreamId: snapshot.StreamId,
                Date: snapshot.Date,
                OriginalAmount: snapshot.Amount,
                OriginalCurrency: "USD",
                UsdAmount: snapshot.Amount,
                ExchangeRate: 1.0m,
                RateSource: "Claude CSV Import"), ct);

            if (recordResult.IsFailed)
            {
                warnings.Add($"Failed to record snapshot for stream {snapshot.StreamId} on {snapshot.Date}: {recordResult.Errors.First().Message}");
                continue;
            }

            snapshotsCreated++;
        }

        return new CsvImportResult(
            TransactionsProcessed: transactions.Count,
            SnapshotsCreated: snapshotsCreated,
            NewStreamsCreated: newStreamsCreated,
            Warnings: warnings);
    }

    public Result<IReadOnlyList<ClassifiedTransaction>> ValidateClassifiedCsv(
        string providerId,
        string csvContent)
    {
        var connector = connectorRegistry.GetCsvImportById(providerId);
        if (connector is null)
            return Result.Fail<IReadOnlyList<ClassifiedTransaction>>($"CSV import connector '{providerId}' not found");

        return connector.ParseClassifiedCsv(csvContent);
    }

    private static List<AggregatedSnapshot> AggregateTransactions(
        IReadOnlyList<ClassifiedTransaction> transactions,
        ImportAggregationMode mode,
        Dictionary<string, string> newStreamIdMap)
    {
        var snapshots = new List<AggregatedSnapshot>();

        // Resolve stream IDs (either existing or from new stream map)
        var resolvedTransactions = transactions
            .Where(t => !string.IsNullOrEmpty(t.StreamId) || (!string.IsNullOrEmpty(t.SuggestedNewStreamName) && newStreamIdMap.ContainsKey(t.SuggestedNewStreamName)))
            .Select(t => new
            {
                Transaction = t,
                ResolvedStreamId = !string.IsNullOrEmpty(t.StreamId) ? t.StreamId : newStreamIdMap.GetValueOrDefault(t.SuggestedNewStreamName ?? "")
            })
            .Where(x => !string.IsNullOrEmpty(x.ResolvedStreamId))
            .ToList();

        switch (mode)
        {
            case ImportAggregationMode.Individual:
                snapshots.AddRange(resolvedTransactions.Select(x => new AggregatedSnapshot(
                    x.ResolvedStreamId!,
                    x.Transaction.Date,
                    x.Transaction.Amount,
                    x.Transaction.Description)));
                break;

            case ImportAggregationMode.DailyPerStream:
                var dailyGroups = resolvedTransactions
                    .GroupBy(x => (x.ResolvedStreamId, x.Transaction.Date));

                foreach (var group in dailyGroups)
                {
                    var totalAmount = group.Sum(x => x.Transaction.Amount);
                    var descriptions = string.Join("; ", group.Select(x => x.Transaction.Description).Distinct().Take(3));
                    snapshots.Add(new AggregatedSnapshot(
                        group.Key.ResolvedStreamId!,
                        group.Key.Date,
                        totalAmount,
                        descriptions));
                }
                break;

            case ImportAggregationMode.MonthlyPerStream:
                var monthlyGroups = resolvedTransactions
                    .GroupBy(x => (x.ResolvedStreamId, Year: x.Transaction.Date.Year, Month: x.Transaction.Date.Month));

                foreach (var group in monthlyGroups)
                {
                    var totalAmount = group.Sum(x => x.Transaction.Amount);
                    var lastDayOfMonth = new DateOnly(group.Key.Year, group.Key.Month, DateTime.DaysInMonth(group.Key.Year, group.Key.Month));
                    snapshots.Add(new AggregatedSnapshot(
                        group.Key.ResolvedStreamId!,
                        lastDayOfMonth,
                        totalAmount,
                        $"Monthly total ({group.Count()} transactions)"));
                }
                break;
        }

        return snapshots;
    }

    private sealed record AggregatedSnapshot(
        string StreamId,
        DateOnly Date,
        decimal Amount,
        string Description);
}
