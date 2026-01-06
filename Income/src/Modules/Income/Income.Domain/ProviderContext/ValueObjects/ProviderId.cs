namespace Income.Domain.ProviderContext.ValueObjects;

internal readonly record struct ProviderId(string Value)
{
    public static ProviderId New() => new(Guid.NewGuid().ToString("N"));
    public static ProviderId From(string value) => new(value);
    public override string ToString() => Value;
}
