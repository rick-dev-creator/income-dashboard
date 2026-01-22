using FluentResults;
using Income.Application.Connectors;
using Income.Application.Services.Streams;

namespace Income.Application.Services.CsvImport;

/// <summary>
/// Service for managing CSV import workflow.
/// </summary>
public interface ICsvImportService
{
    /// <summary>
    /// Exports existing streams in a format suitable for Claude Code classification.
    /// </summary>
    Task<Result<StreamExportPackage>> ExportStreamsForClassificationAsync(
        string providerId,
        CancellationToken ct = default);

    /// <summary>
    /// Imports classified transactions and creates snapshots.
    /// Also creates new streams for transactions that couldn't be mapped.
    /// </summary>
    Task<Result<CsvImportResult>> ImportClassifiedTransactionsAsync(
        string providerId,
        IReadOnlyList<ClassifiedTransaction> transactions,
        IReadOnlyList<NewStreamRequest> newStreams,
        ImportAggregationMode aggregationMode,
        CancellationToken ct = default);

    /// <summary>
    /// Validates classified CSV content against a connector.
    /// </summary>
    Result<IReadOnlyList<ClassifiedTransaction>> ValidateClassifiedCsv(
        string providerId,
        string csvContent);
}

/// <summary>
/// Package containing everything needed for Claude Code classification.
/// </summary>
public sealed record StreamExportPackage(
    IReadOnlyList<StreamExportItem> Streams,
    string ClassificationInstructions,
    string ExpectedOutputFormat,
    string BankFormatInfo);

/// <summary>
/// Request to create a new stream during import.
/// </summary>
public sealed record NewStreamRequest(
    string Name,
    string Category,
    StreamTypeDto StreamType);

/// <summary>
/// How to aggregate transactions into snapshots.
/// </summary>
public enum ImportAggregationMode
{
    /// <summary>Each transaction becomes a snapshot</summary>
    Individual,

    /// <summary>Aggregate by day per stream</summary>
    DailyPerStream,

    /// <summary>Aggregate by month per stream</summary>
    MonthlyPerStream
}

/// <summary>
/// Result of a CSV import operation.
/// </summary>
public sealed record CsvImportResult(
    int TransactionsProcessed,
    int SnapshotsCreated,
    int NewStreamsCreated,
    IReadOnlyList<string> Warnings);
