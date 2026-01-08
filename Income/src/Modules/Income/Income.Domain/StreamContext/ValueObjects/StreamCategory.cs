namespace Income.Domain.StreamContext.ValueObjects;

internal sealed record StreamCategory
{
    public static readonly StreamCategory Trading = new("Trading");
    public static readonly StreamCategory Referral = new("Referral");
    public static readonly StreamCategory Subscription = new("Subscription");
    public static readonly StreamCategory Salary = new("Salary");
    public static readonly StreamCategory Rental = new("Rental");
    public static readonly StreamCategory Dividend = new("Dividend");
    public static readonly StreamCategory Other = new("Other");

    private static readonly Dictionary<string, StreamCategory> All = new(StringComparer.OrdinalIgnoreCase)
    {
        [Trading.Value] = Trading,
        [Referral.Value] = Referral,
        [Subscription.Value] = Subscription,
        [Salary.Value] = Salary,
        [Rental.Value] = Rental,
        [Dividend.Value] = Dividend,
        [Other.Value] = Other
    };

    public string Value { get; }

    private StreamCategory(string value) => Value = value;

    public static StreamCategory? FromString(string value) =>
        All.TryGetValue(value, out var category) ? category : null;

    public static StreamCategory FromStringOrDefault(string value) =>
        FromString(value) ?? Other;

    public static IEnumerable<StreamCategory> GetAll() => All.Values;

    public override string ToString() => Value;
}
