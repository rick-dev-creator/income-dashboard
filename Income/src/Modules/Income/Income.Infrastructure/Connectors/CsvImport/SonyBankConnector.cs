using System.Globalization;
using System.Text;
using FluentResults;
using Income.Application.Connectors;

namespace Income.Infrastructure.Connectors.CsvImport;

/// <summary>
/// CSV import connector for Sony Bank Japan.
/// Handles parsing of Sony Bank CSV exports and generating classification instructions.
/// </summary>
internal sealed class SonyBankConnector : ICsvImportConnector
{
    public string ProviderId => CsvImportProviders.SonyBankJapan;
    public string DisplayName => "Sony Bank Japan";
    public string ProviderType => "Bank";
    public ConnectorKind Kind => ConnectorKind.CsvImport;
    public string DefaultCurrency => "JPY";
    public SupportedStreamTypes SupportedStreamTypes => SupportedStreamTypes.Both;
    public string BankName => "Sony Bank (Japan)";

    public BankCsvFormatSpec FormatSpec => new(
        DateColumn: "取引日",
        DateFormat: "yyyy/MM/dd",
        DescriptionColumn: "摘要",
        AmountColumn: null,
        DepositColumn: "入金額",
        WithdrawalColumn: "出金額",
        Encoding: "Shift_JIS",
        Delimiter: ",",
        HasHeader: true,
        SkipRows: 0);

    public Result<IReadOnlyList<BankTransaction>> ParseBankCsv(string csvContent)
    {
        var transactions = new List<BankTransaction>();
        var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
            return Result.Fail("CSV file is empty");

        // Find header line and column indices
        var headerLine = lines[FormatSpec.SkipRows];
        var headers = ParseCsvLine(headerLine);

        var dateIdx = Array.FindIndex(headers, h => h == FormatSpec.DateColumn);
        var descIdx = Array.FindIndex(headers, h => h == FormatSpec.DescriptionColumn);
        var depositIdx = FormatSpec.DepositColumn != null
            ? Array.FindIndex(headers, h => h == FormatSpec.DepositColumn)
            : -1;
        var withdrawalIdx = FormatSpec.WithdrawalColumn != null
            ? Array.FindIndex(headers, h => h == FormatSpec.WithdrawalColumn)
            : -1;

        if (dateIdx < 0 || descIdx < 0)
            return Result.Fail($"Required columns not found. Expected: {FormatSpec.DateColumn}, {FormatSpec.DescriptionColumn}");

        // Parse data lines
        for (var i = FormatSpec.SkipRows + 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = ParseCsvLine(line);
            if (fields.Length <= Math.Max(dateIdx, Math.Max(descIdx, Math.Max(depositIdx, withdrawalIdx))))
                continue;

            if (!DateOnly.TryParseExact(fields[dateIdx], FormatSpec.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;

            var description = fields[descIdx];
            decimal amount = 0;
            bool isDeposit;

            // Sony Bank has separate deposit/withdrawal columns
            if (depositIdx >= 0 && withdrawalIdx >= 0)
            {
                var depositStr = fields[depositIdx].Replace(",", "").Trim();
                var withdrawalStr = fields[withdrawalIdx].Replace(",", "").Trim();

                if (!string.IsNullOrEmpty(depositStr) && decimal.TryParse(depositStr, out var deposit))
                {
                    amount = deposit;
                    isDeposit = true;
                }
                else if (!string.IsNullOrEmpty(withdrawalStr) && decimal.TryParse(withdrawalStr, out var withdrawal))
                {
                    amount = withdrawal;
                    isDeposit = false;
                }
                else
                {
                    continue; // Skip rows with no amount
                }
            }
            else
            {
                continue; // Invalid format
            }

            transactions.Add(new BankTransaction(date, description, amount, isDeposit, line));
        }

        return transactions;
    }

    public string GetClassificationInstructions(IReadOnlyList<StreamExportItem> existingStreams)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Bank Transaction Classification Instructions");
        sb.AppendLine();
        sb.AppendLine("You are helping classify bank transactions from Sony Bank Japan.");
        sb.AppendLine();
        sb.AppendLine("## CRITICAL: Life Economics Model - Only 4 Streams");
        sb.AppendLine();
        sb.AppendLine("This app uses a simplified 'Life Economics' model with ONLY 4 streams:");
        sb.AppendLine("1. **Income** - ALL money coming in");
        sb.AppendLine("2. **Fixed** - ALL fixed monthly expenses");
        sb.AppendLine("3. **Variable** - ALL variable daily expenses");
        sb.AppendLine("4. **Savings** - ALL money set aside");
        sb.AppendLine();
        sb.AppendLine("**DO NOT create granular streams** like 'Netflix', 'Amazon', 'Uber', etc.");
        sb.AppendLine("**ALL transactions must go into one of these 4 streams.**");
        sb.AppendLine();

        sb.AppendLine("## IMPORTANT: Currency Conversion");
        sb.AppendLine();
        sb.AppendLine("**The system uses USD as its base currency.**");
        sb.AppendLine();
        sb.AppendLine("1. All amounts in the bank CSV are in **Japanese Yen (JPY)**");
        sb.AppendLine("2. You MUST convert all amounts from JPY to USD");
        sb.AppendLine("3. **Search the web for the current JPY/USD exchange rate** before processing");
        sb.AppendLine("4. Use the rate for the transaction date if available, otherwise use today's rate");
        sb.AppendLine("5. Round USD amounts to 2 decimal places");
        sb.AppendLine();
        sb.AppendLine("Example: If ¥15,000 JPY and rate is 0.0067 USD/JPY → Amount = 100.50");
        sb.AppendLine();

        sb.AppendLine("## Existing Streams");
        sb.AppendLine();
        sb.AppendLine("| StreamId | Name | Category | Type | Keywords |");
        sb.AppendLine("|----------|------|----------|------|----------|");

        foreach (var stream in existingStreams)
        {
            sb.AppendLine($"| {stream.StreamId} | {stream.StreamName} | {stream.Category} | {stream.StreamType} | {stream.MatchingKeywords ?? "-"} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Classification Rules");
        sb.AppendLine();
        sb.AppendLine("1. **Deposits (入金)** → Assign to stream named `Income`");
        sb.AppendLine("2. **Fixed expenses** (rent, utilities, subscriptions, insurance) → Assign to stream named `Fixed`");
        sb.AppendLine("3. **Variable expenses** (food, shopping, transport, entertainment) → Assign to stream named `Variable`");
        sb.AppendLine("4. **Savings/Investments** (transfers to savings, investments) → Assign to stream named `Savings`");
        sb.AppendLine();
        sb.AppendLine("**How to decide Fixed vs Variable:**");
        sb.AppendLine("- Fixed = same amount every month (Netflix, rent, gym membership)");
        sb.AppendLine("- Variable = fluctuates (groceries, restaurants, Uber, Amazon)");
        sb.AppendLine();

        sb.AppendLine("## The 4 Streams (Category = Stream Name)");
        sb.AppendLine();
        sb.AppendLine("| Stream/Category | Use For | Transaction Examples |");
        sb.AppendLine("|-----------------|---------|---------------------|");
        sb.AppendLine("| `Income` | All money coming IN | Salary, freelance, bonuses, refunds, transfers received |");
        sb.AppendLine("| `Fixed` | Fixed monthly costs | Rent, utilities, Netflix, Spotify, insurance, gym membership |");
        sb.AppendLine("| `Variable` | Variable daily expenses | Groceries, restaurants, Uber, Amazon, shopping, entertainment |");
        sb.AppendLine("| `Savings` | Money set aside | Savings transfers, investments, crypto purchases |");
        sb.AppendLine();
        sb.AppendLine("**SuggestedNewStreamName and SuggestedCategory must ALWAYS be one of: Income, Fixed, Variable, or Savings**");
        sb.AppendLine();

        sb.AppendLine("## Expected Output Format");
        sb.AppendLine();
        sb.AppendLine("Return a CSV with this exact header:");
        sb.AppendLine("```");
        sb.AppendLine(GetClassifiedCsvHeader());
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Output Column Descriptions");
        sb.AppendLine();
        sb.AppendLine("- **Date**: YYYY-MM-DD format");
        sb.AppendLine("- **Description**: Original transaction description");
        sb.AppendLine("- **Amount**: Amount converted to USD (2 decimal places, no currency symbol)");
        sb.AppendLine("- **IsDeposit**: true for income, false for outcome");
        sb.AppendLine("- **StreamId**: ID of matched stream, or empty if new");
        sb.AppendLine("- **StreamName**: Name of matched stream, or empty if new");
        sb.AppendLine("- **SuggestedNewStreamName**: Name for new stream (only if RequiresNewStream=true)");
        sb.AppendLine("- **SuggestedCategory**: Category for new stream (ONLY: Income, Fixed, Variable, or Savings)");
        sb.AppendLine("- **RequiresNewStream**: true if no existing stream matches");
        sb.AppendLine();

        sb.AppendLine("## Example Output");
        sb.AppendLine();
        sb.AppendLine("Assuming JPY/USD rate of 0.0067:");
        sb.AppendLine();
        sb.AppendLine("```csv");
        sb.AppendLine(GetClassifiedCsvHeader());
        sb.AppendLine("2024-01-15,AMAZON.CO.JP,16.75,false,,,Variable,Variable,true");
        sb.AppendLine("2024-01-16,振込 ﾀﾅｶ ﾀﾛｳ,1005.00,true,,,Income,Income,true");
        sb.AppendLine("2024-01-17,NETFLIX,15.99,false,,,Fixed,Fixed,true");
        sb.AppendLine("2024-01-18,ATM振込 貯金,500.00,false,,,Savings,Savings,true");
        sb.AppendLine("2024-01-19,スーパー,45.50,false,,,Variable,Variable,true");
        sb.AppendLine("2024-01-20,電気代,80.00,false,,,Fixed,Fixed,true");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("**Note:** Stream name = Category name. All transactions go to one of 4 streams.");
        sb.AppendLine();
        sb.AppendLine("(¥2,500 → $16.75 USD, ¥150,000 → $1,005.00 USD)");

        return sb.ToString();
    }

    public string GetClassifiedCsvHeader()
    {
        return "Date,Description,Amount,IsDeposit,StreamId,StreamName,SuggestedNewStreamName,SuggestedCategory,RequiresNewStream";
    }

    public Result<IReadOnlyList<ClassifiedTransaction>> ParseClassifiedCsv(string csvContent)
    {
        var transactions = new List<ClassifiedTransaction>();
        var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
            return Result.Fail("CSV must have header and at least one data row");

        // Validate header
        var expectedHeader = GetClassifiedCsvHeader();
        if (!lines[0].Trim().Equals(expectedHeader, StringComparison.OrdinalIgnoreCase))
            return Result.Fail($"Invalid header. Expected: {expectedHeader}");

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = ParseCsvLine(line);
            if (fields.Length < 9)
            {
                return Result.Fail($"Line {i + 1}: Expected 9 columns, got {fields.Length}");
            }

            if (!DateOnly.TryParse(fields[0], out var date))
                return Result.Fail($"Line {i + 1}: Invalid date format '{fields[0]}'");

            if (!decimal.TryParse(fields[2], out var amount))
                return Result.Fail($"Line {i + 1}: Invalid amount '{fields[2]}'");

            if (!bool.TryParse(fields[3], out var isDeposit))
                return Result.Fail($"Line {i + 1}: Invalid IsDeposit value '{fields[3]}'");

            var streamId = string.IsNullOrWhiteSpace(fields[4]) ? null : fields[4].Trim();

            if (!bool.TryParse(fields[8], out var requiresNewStream))
                return Result.Fail($"Line {i + 1}: Invalid RequiresNewStream value '{fields[8]}'");

            transactions.Add(new ClassifiedTransaction(
                Date: date,
                Description: fields[1],
                Amount: amount,
                IsDeposit: isDeposit,
                StreamId: streamId,
                StreamName: string.IsNullOrWhiteSpace(fields[5]) ? null : fields[5],
                SuggestedNewStreamName: string.IsNullOrWhiteSpace(fields[6]) ? null : fields[6],
                SuggestedCategory: string.IsNullOrWhiteSpace(fields[7]) ? null : fields[7],
                RequiresNewStream: requiresNewStream
            ));
        }

        return transactions;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString().Trim());
        return [.. fields];
    }
}
