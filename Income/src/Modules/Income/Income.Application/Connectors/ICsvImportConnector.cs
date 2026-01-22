using FluentResults;

namespace Income.Application.Connectors;

/// <summary>
/// Well-known provider IDs for CSV import bank connectors.
/// </summary>
public static class CsvImportProviders
{
    /// <summary>
    /// Provider ID for Sony Bank Japan CSV imports.
    /// </summary>
    public const string SonyBankJapan = "csv-sonybank-jp";
}

/// <summary>
/// Connector that imports transactions from bank CSV files.
/// The workflow is:
/// 1. User exports existing streams from FlowMetrics
/// 2. User uses Claude Code to classify bank CSV against existing streams
/// 3. User imports the classified CSV back into FlowMetrics
/// </summary>
public interface ICsvImportConnector : IIncomeConnector
{
    /// <summary>
    /// Human-readable bank name for display in UI.
    /// </summary>
    string BankName { get; }

    /// <summary>
    /// Specification of the expected CSV format from this bank.
    /// Includes column names, date formats, amount formats, etc.
    /// </summary>
    BankCsvFormatSpec FormatSpec { get; }

    /// <summary>
    /// Parses raw bank CSV content into transactions.
    /// Returns transactions with original bank data, ready for classification.
    /// </summary>
    Result<IReadOnlyList<BankTransaction>> ParseBankCsv(string csvContent);

    /// <summary>
    /// Generates classification instructions for Claude Code.
    /// Includes expected output format, stream matching rules, and examples.
    /// </summary>
    string GetClassificationInstructions(IReadOnlyList<StreamExportItem> existingStreams);

    /// <summary>
    /// Parses a classified CSV (output from Claude Code) into import-ready data.
    /// </summary>
    Result<IReadOnlyList<ClassifiedTransaction>> ParseClassifiedCsv(string csvContent);

    /// <summary>
    /// Gets the expected CSV header for the classified output format.
    /// </summary>
    string GetClassifiedCsvHeader();
}

/// <summary>
/// Specification of a bank's CSV format.
/// </summary>
public sealed record BankCsvFormatSpec(
    string DateColumn,
    string DateFormat,
    string DescriptionColumn,
    string? AmountColumn,
    string? DepositColumn,
    string? WithdrawalColumn,
    string Encoding,
    string Delimiter,
    bool HasHeader,
    int SkipRows);

/// <summary>
/// A transaction parsed from raw bank CSV.
/// </summary>
public sealed record BankTransaction(
    DateOnly Date,
    string Description,
    decimal Amount,
    bool IsDeposit,
    string OriginalLine);

/// <summary>
/// Stream information exported for Claude Code classification.
/// </summary>
public sealed record StreamExportItem(
    string StreamId,
    string StreamName,
    string Category,
    string StreamType,
    string? MatchingKeywords);

/// <summary>
/// A transaction that has been classified by Claude Code.
/// </summary>
public sealed record ClassifiedTransaction(
    DateOnly Date,
    string Description,
    decimal Amount,
    bool IsDeposit,
    string? StreamId,
    string? StreamName,
    string? SuggestedNewStreamName,
    string? SuggestedCategory,
    bool RequiresNewStream);
