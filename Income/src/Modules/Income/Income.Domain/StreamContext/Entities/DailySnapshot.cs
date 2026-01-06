using Domain.Shared.Kernel;
using Income.Domain.StreamContext.ValueObjects;

namespace Income.Domain.StreamContext.Entities;

internal sealed class DailySnapshot : Entity<SnapshotId>
{
    private DailySnapshot() { }

    private DailySnapshot(
        SnapshotId id,
        DateOnly date,
        decimal originalAmount,
        string originalCurrency,
        decimal usdAmount,
        decimal exchangeRate,
        string rateSource,
        DateTime snapshotAt)
    {
        Id = id;
        Date = date;
        OriginalAmount = originalAmount;
        OriginalCurrency = originalCurrency;
        UsdAmount = usdAmount;
        ExchangeRate = exchangeRate;
        RateSource = rateSource;
        SnapshotAt = snapshotAt;
    }

    public DateOnly Date { get; private init; }

    public decimal OriginalAmount
    {
        get;
        private set => field = value >= 0 ? value
            : throw new DomainException("Amount cannot be negative");
    }

    public string OriginalCurrency { get; private init; } = null!;
    public decimal UsdAmount { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public string RateSource { get; private set; } = null!;
    public DateTime SnapshotAt { get; private init; }

    internal static DailySnapshot Create(
        DateOnly date,
        Money originalMoney,
        ExchangeConversion conversion)
    {
        return new DailySnapshot(
            id: SnapshotId.New(),
            date: date,
            originalAmount: originalMoney.Amount,
            originalCurrency: originalMoney.Currency,
            usdAmount: conversion.UsdMoney.Amount,
            exchangeRate: conversion.Rate,
            rateSource: conversion.Source,
            snapshotAt: DateTime.UtcNow);
    }

    internal static DailySnapshot Reconstruct(
        SnapshotId id,
        DateOnly date,
        decimal originalAmount,
        string originalCurrency,
        decimal usdAmount,
        decimal exchangeRate,
        string rateSource,
        DateTime snapshotAt)
    {
        return new DailySnapshot(
            id, date, originalAmount, originalCurrency,
            usdAmount, exchangeRate, rateSource, snapshotAt);
    }

    internal void UpdateAmount(Money newAmount, ExchangeConversion conversion)
    {
        OriginalAmount = newAmount.Amount;
        UsdAmount = conversion.UsdMoney.Amount;
        ExchangeRate = conversion.Rate;
        RateSource = conversion.Source;
    }

    public Money GetOriginalMoney() => Money.Create(OriginalAmount, OriginalCurrency).Value;
    public Money GetUsdMoney() => Money.Usd(UsdAmount);
}
