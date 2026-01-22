using Domain.Shared.Kernel;
using FluentResults;
using Income.Domain.ProviderContext.ValueObjects;

namespace Income.Domain.ProviderContext.Aggregates;

internal enum ProviderType
{
    Exchange,
    Creator,
    Payment,
    Manual
}

internal enum SyncFrequency
{
    Realtime,
    Hourly,
    Daily,
    Manual
}

internal enum ConnectorKind
{
    Recurring,  // Auto-generates snapshots based on schedule (Salary, Rent)
    Syncable,   // Pulls data from external APIs (Binance, Patreon)
    CsvImport   // Imports transactions from bank CSV files
}

internal sealed class Provider : AggregateRoot<ProviderId>
{
    private Provider(
        ProviderId id,
        string name,
        ProviderType type,
        ConnectorKind connectorKind,
        string defaultCurrency,
        SyncFrequency syncFrequency,
        string? configSchema)
    {
        Id = id;
        _name = name;
        Type = type;
        ConnectorKind = connectorKind;
        DefaultCurrency = defaultCurrency;
        SyncFrequency = syncFrequency;
        ConfigSchema = configSchema;
    }

    private string _name = null!;
    public string Name
    {
        get => _name;
        private set => _name = !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new DomainException("Provider name is required");
    }

    public ProviderType Type { get; private init; }
    public ConnectorKind ConnectorKind { get; private init; }
    public string DefaultCurrency { get; private init; } = null!;
    public SyncFrequency SyncFrequency { get; private init; }
    public string? ConfigSchema { get; private init; }

    internal static Result<Provider> Create(
        string name,
        ProviderType type,
        ConnectorKind connectorKind,
        string defaultCurrency,
        SyncFrequency syncFrequency,
        string? configSchema = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail<Provider>("Provider name is required");

        if (string.IsNullOrWhiteSpace(defaultCurrency))
            return Result.Fail<Provider>("Default currency is required");

        var provider = new Provider(
            id: ProviderId.New(),
            name: name,
            type: type,
            connectorKind: connectorKind,
            defaultCurrency: defaultCurrency.ToUpperInvariant(),
            syncFrequency: syncFrequency,
            configSchema: configSchema);

        return Result.Ok(provider);
    }

    internal static Provider Reconstruct(
        ProviderId id,
        string name,
        ProviderType type,
        ConnectorKind connectorKind,
        string defaultCurrency,
        SyncFrequency syncFrequency,
        string? configSchema)
    {
        return new Provider(id, name, type, connectorKind, defaultCurrency, syncFrequency, configSchema);
    }

    public bool RequiresCredentials => ConnectorKind == ConnectorKind.Syncable;
    public bool SupportsAutoSync => ConnectorKind == ConnectorKind.Syncable;
    public bool IsRecurring => ConnectorKind == ConnectorKind.Recurring;
}
