using Income.Application.Connectors;

namespace Income.Infrastructure.Connectors.BuiltIn;

/// <summary>
/// Built-in connector for fixed/recurring income like salary, rent, subscriptions.
/// Generates snapshots automatically based on schedule without external API calls.
/// </summary>
internal sealed class FixedIncomeConnector : IRecurringConnector
{
    public string ProviderId => "fixed-income";
    public string DisplayName => "Fixed Income (Salary, Rent, etc.)";
    public string ProviderType => "Manual";
    public ConnectorKind Kind => ConnectorKind.Recurring;
    public string DefaultCurrency => "USD";

    public DateOnly CalculateNextPaymentDate(
        DateOnly startDate,
        RecurringFrequency frequency,
        DateOnly asOfDate)
    {
        var nextDate = startDate;

        // Move forward until we find a date >= asOfDate
        while (nextDate < asOfDate)
        {
            nextDate = AddFrequencyInterval(nextDate, frequency);
        }

        return nextDate;
    }

    public bool IsPaymentDue(
        DateOnly startDate,
        RecurringFrequency frequency,
        DateOnly checkDate)
    {
        var current = startDate;

        // Check if checkDate matches any payment date in the schedule
        while (current <= checkDate)
        {
            if (current == checkDate)
                return true;

            current = AddFrequencyInterval(current, frequency);
        }

        return false;
    }

    public RecurringSnapshotData GenerateSnapshot(
        decimal amount,
        string currency,
        DateOnly paymentDate)
    {
        // For fixed income, amount is already in the specified currency
        // Exchange rate is 1.0 if same as USD, otherwise needs conversion
        var isUsd = currency.Equals("USD", StringComparison.OrdinalIgnoreCase);

        return new RecurringSnapshotData(
            Date: paymentDate,
            OriginalAmount: amount,
            OriginalCurrency: currency,
            UsdAmount: isUsd ? amount : amount, // TODO: Integrate with exchange rate service
            ExchangeRate: isUsd ? 1.0m : 1.0m,  // TODO: Integrate with exchange rate service
            RateSource: isUsd ? "Fixed" : "Fixed"
        );
    }

    private static DateOnly AddFrequencyInterval(DateOnly date, RecurringFrequency frequency)
    {
        return frequency switch
        {
            RecurringFrequency.Weekly => date.AddDays(7),
            RecurringFrequency.BiWeekly => date.AddDays(14),
            RecurringFrequency.Monthly => date.AddMonths(1),
            RecurringFrequency.Quarterly => date.AddMonths(3),
            RecurringFrequency.Yearly => date.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unknown frequency")
        };
    }
}
