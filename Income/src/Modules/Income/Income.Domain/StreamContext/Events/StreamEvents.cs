using Domain.Shared.Kernel;
using Income.Domain.ProviderContext.ValueObjects;
using Income.Domain.StreamContext.ValueObjects;

namespace Income.Domain.StreamContext.Events;

internal sealed record StreamCreatedDomainEvent(
    StreamId StreamId,
    ProviderId ProviderId,
    string Name,
    string Category) : DomainEvent;

internal sealed record SnapshotRecordedDomainEvent(
    StreamId StreamId,
    SnapshotId SnapshotId,
    DateOnly Date,
    decimal UsdAmount) : DomainEvent;

internal sealed record SnapshotUpdatedDomainEvent(
    StreamId StreamId,
    SnapshotId SnapshotId,
    DateOnly Date,
    decimal UsdAmount) : DomainEvent;

internal sealed record StreamSyncFailedDomainEvent(
    StreamId StreamId,
    string Error) : DomainEvent;

internal sealed record StreamDisabledDomainEvent(
    StreamId StreamId) : DomainEvent;

internal sealed record StreamEnabledDomainEvent(
    StreamId StreamId) : DomainEvent;
