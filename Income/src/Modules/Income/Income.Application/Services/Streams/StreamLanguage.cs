namespace Income.Application.Services.Streams;

/// <summary>
/// Contains all text labels that change based on stream type (Income vs Outcome).
/// This enables the same components to display context-appropriate language.
/// </summary>
public sealed record StreamLanguage(
    // Page titles
    string StreamsTitle,           // "Income Streams" / "Outcome Streams"
    string StreamsSubtitle,        // "Manage your income sources" / "Manage your expenses"

    // Dashboard labels
    string DailyRateLabel,         // "Daily Income Rate" / "Daily Burn Rate"
    string TotalLabel,             // "Total Income" / "Total Expenses"
    string StreamCountLabel,       // "income sources" / "expense sources"

    // Actions
    string AddButtonText,          // "Add Income Source" / "Add Expense"
    string AddDialogTitle,         // "Add Income Stream" / "Add Outcome Stream"

    // Trends - Note: meaning is INVERTED for outcomes
    string TrendGoodLabel,         // "Growing" / "Decreasing" (good for income, good for outcome to decrease)
    string TrendBadLabel,          // "Declining" / "Increasing" (bad for income, bad for outcome to increase)
    bool InvertTrendSentiment,     // false for income, true for outcome

    // Stream health
    string HealthGoodLabel,        // "Healthy" / "Under Control"
    string HealthWarningLabel,     // "Attention Needed" / "Watch Spending"
    string HealthBadLabel,         // "At Risk" / "Overspending"

    // Analytics
    string ProjectionLabel,        // "Projected Income" / "Projected Expenses"
    string StabilityLabel,         // "Income Stability" / "Expense Predictability"

    // Verbs
    string GeneratingVerb,         // "generating" / "spending"
    string EarnedVerb,             // "earned" / "spent"
    string ReceivedVerb,           // "received" / "paid"

    // Colors/Icons
    string AccentColor,            // "#10b981" (green) / "#ef4444" (red)
    string BackgroundTint          // "rgba(16,185,129,0.02)" / "rgba(239,68,68,0.02)"
)
{
    /// <summary>
    /// Language configuration for Income streams.
    /// </summary>
    public static StreamLanguage Income => new(
        StreamsTitle: "Income Streams",
        StreamsSubtitle: "Manage your income sources and track earnings",
        DailyRateLabel: "Daily Income Rate",
        TotalLabel: "Total Income",
        StreamCountLabel: "income sources",
        AddButtonText: "Add Income Source",
        AddDialogTitle: "Add Income Stream",
        TrendGoodLabel: "Growing",
        TrendBadLabel: "Declining",
        InvertTrendSentiment: false,
        HealthGoodLabel: "Healthy",
        HealthWarningLabel: "Attention Needed",
        HealthBadLabel: "At Risk",
        ProjectionLabel: "Projected Income",
        StabilityLabel: "Income Stability",
        GeneratingVerb: "generating",
        EarnedVerb: "earned",
        ReceivedVerb: "received",
        AccentColor: "#10b981",
        BackgroundTint: "rgba(16,185,129,0.02)"
    );

    /// <summary>
    /// Language configuration for Outcome streams.
    /// </summary>
    public static StreamLanguage Outcome => new(
        StreamsTitle: "Outcome Streams",
        StreamsSubtitle: "Manage your expenses and track spending",
        DailyRateLabel: "Daily Burn Rate",
        TotalLabel: "Total Expenses",
        StreamCountLabel: "expense sources",
        AddButtonText: "Add Expense",
        AddDialogTitle: "Add Outcome Stream",
        TrendGoodLabel: "Decreasing",
        TrendBadLabel: "Increasing",
        InvertTrendSentiment: true,
        HealthGoodLabel: "Under Control",
        HealthWarningLabel: "Watch Spending",
        HealthBadLabel: "Overspending",
        ProjectionLabel: "Projected Expenses",
        StabilityLabel: "Expense Predictability",
        GeneratingVerb: "spending",
        EarnedVerb: "spent",
        ReceivedVerb: "paid",
        AccentColor: "#ef4444",
        BackgroundTint: "rgba(239,68,68,0.02)"
    );

    /// <summary>
    /// Gets the appropriate language for the given stream type.
    /// </summary>
    public static StreamLanguage ForType(StreamTypeDto streamType) => streamType switch
    {
        StreamTypeDto.Income => Income,
        StreamTypeDto.Outcome => Outcome,
        _ => Income
    };
}
