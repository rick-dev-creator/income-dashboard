namespace Income.Domain.Tests.StreamContext.ValueObjects;

public class SyncStatusTests
{
    [Fact]
    public void Initial_ShouldBeActive()
    {
        // Act
        var status = SyncStatus.Initial();

        // Assert
        status.State.ShouldBe(SyncState.Active);
        status.CanSync.ShouldBeTrue();
        status.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    public void MarkSyncing_ShouldChangeSateToSyncing()
    {
        // Arrange
        var status = SyncStatus.Initial();

        // Act
        var syncing = status.MarkSyncing();

        // Assert
        syncing.State.ShouldBe(SyncState.Syncing);
        syncing.LastAttemptAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkSuccess_ShouldClearError()
    {
        // Arrange
        var status = SyncStatus.Initial()
            .MarkFailed("Some error");

        // Act
        var success = status.MarkSuccess();

        // Assert
        success.State.ShouldBe(SyncState.Active);
        success.LastError.ShouldBeNull();
        success.LastSuccessAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkFailed_ShouldSetError()
    {
        // Arrange
        var status = SyncStatus.Initial();

        // Act
        var failed = status.MarkFailed("Connection timeout");

        // Assert
        failed.State.ShouldBe(SyncState.Failed);
        failed.LastError.ShouldBe("Connection timeout");
        failed.CanSync.ShouldBeTrue();
        failed.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    public void Disable_ShouldPreventSync()
    {
        // Arrange
        var status = SyncStatus.Initial();

        // Act
        var disabled = status.Disable();

        // Assert
        disabled.State.ShouldBe(SyncState.Disabled);
        disabled.CanSync.ShouldBeFalse();
        disabled.NextScheduledAt.ShouldBeNull();
    }

    [Fact]
    public void Enable_ShouldAllowSync()
    {
        // Arrange
        var status = SyncStatus.Initial().Disable();

        // Act
        var enabled = status.Enable();

        // Assert
        enabled.State.ShouldBe(SyncState.Active);
        enabled.CanSync.ShouldBeTrue();
    }
}
