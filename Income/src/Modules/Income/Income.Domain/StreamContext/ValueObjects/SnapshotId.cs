namespace Income.Domain.StreamContext.ValueObjects;

internal readonly record struct SnapshotId(string Value)
{
    public static SnapshotId New() => new(Guid.NewGuid().ToString("N"));
    public static SnapshotId From(string value) => new(value);
    public override string ToString() => Value;
}
