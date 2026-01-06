namespace Income.Domain.StreamContext.ValueObjects;

internal readonly record struct StreamId(string Value)
{
    public static StreamId New() => new(Guid.NewGuid().ToString("N"));
    public static StreamId From(string value) => new(value);
    public override string ToString() => Value;
}
