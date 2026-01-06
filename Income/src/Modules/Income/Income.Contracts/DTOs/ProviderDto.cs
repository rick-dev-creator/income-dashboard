namespace Income.Contracts.DTOs;

public sealed record ProviderDto(
    string Id,
    string Name,
    string Type,
    string DefaultCurrency,
    string SyncFrequency,
    string? ConfigSchema);
