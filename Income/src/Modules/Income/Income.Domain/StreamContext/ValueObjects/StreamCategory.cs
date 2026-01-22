namespace Income.Domain.StreamContext.ValueObjects;

/// <summary>
/// Global Life Economics categories.
/// Simplified to 4 categories for high-level financial overview.
/// Stream names provide granularity (e.g., "Netflix" under Fixed, "Uber" under Variable).
/// </summary>
internal sealed record StreamCategory
{
    /// <summary>
    /// All money coming in: salary, freelance, bonuses, dividends, refunds, gifts, etc.
    /// </summary>
    public static readonly StreamCategory Income = new("Income");

    /// <summary>
    /// Fixed monthly costs: rent, utilities, subscriptions, insurance, loan payments, etc.
    /// </summary>
    public static readonly StreamCategory Fixed = new("Fixed");

    /// <summary>
    /// Variable expenses: food, dining, transport, shopping, entertainment, healthcare, etc.
    /// </summary>
    public static readonly StreamCategory Variable = new("Variable");

    /// <summary>
    /// Savings and investments: savings transfers, investments, crypto, retirement, etc.
    /// </summary>
    public static readonly StreamCategory Savings = new("Savings");

    private static readonly Dictionary<string, StreamCategory> All = new(StringComparer.OrdinalIgnoreCase)
    {
        [Income.Value] = Income,
        [Fixed.Value] = Fixed,
        [Variable.Value] = Variable,
        [Savings.Value] = Savings
    };

    // Legacy category mappings for backwards compatibility
    private static readonly Dictionary<string, StreamCategory> LegacyMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Old Income categories -> Income
        ["Trading"] = Income,
        ["Investment"] = Savings, // Investments go to Savings
        ["Staking"] = Savings,
        ["Referral"] = Income,
        ["Salary"] = Income,
        ["Freelance"] = Income,
        ["Business"] = Income,
        ["Rental"] = Income,
        ["Dividend"] = Income,
        ["Bonus"] = Income,
        ["Gift"] = Income,
        ["Refund"] = Income,
        // Old Outcome categories -> Fixed or Variable
        ["Housing"] = Fixed,
        ["Utilities"] = Fixed,
        ["Insurance"] = Fixed,
        ["Subscription"] = Fixed,
        ["Software"] = Fixed,
        ["Services"] = Variable,
        ["Food"] = Variable,
        ["Dining"] = Variable,
        ["Transport"] = Variable,
        ["Travel"] = Variable,
        ["Entertainment"] = Variable,
        ["Healthcare"] = Variable,
        ["Education"] = Variable,
        ["Shopping"] = Variable,
        ["Fees"] = Variable,
        ["Taxes"] = Fixed,
        ["Other"] = Variable
    };

    public string Value { get; }

    private StreamCategory(string value) => Value = value;

    public static StreamCategory? FromString(string value)
    {
        if (All.TryGetValue(value, out var category))
            return category;

        // Try legacy mapping
        if (LegacyMappings.TryGetValue(value, out var legacyCategory))
            return legacyCategory;

        return null;
    }

    public static StreamCategory FromStringOrDefault(string value) =>
        FromString(value) ?? Variable;

    public static IEnumerable<StreamCategory> GetAll() => All.Values;

    public override string ToString() => Value;
}
