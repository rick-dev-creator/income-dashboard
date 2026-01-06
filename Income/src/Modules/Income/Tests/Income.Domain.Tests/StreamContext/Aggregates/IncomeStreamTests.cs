using Income.Domain.Tests.TestData;

namespace Income.Domain.Tests.StreamContext.Aggregates;

public class IncomeStreamTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var providerId = ProviderId.New();
        var data = new TestCreateStreamData(
            providerId,
            "Blofin Trading",
            StreamCategory.Trading,
            "USD",
            IsFixed: false,
            FixedPeriod: null);

        // Act
        var result = IncomeStream.Create(data);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ProviderId.ShouldBe(providerId);
        result.Value.Name.ShouldBe("Blofin Trading");
        result.Value.Category.ShouldBe(StreamCategory.Trading);
        result.Value.OriginalCurrency.ShouldBe("USD");
        result.Value.IsFixed.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithFixedIncomeAndPeriod_ShouldSucceed()
    {
        // Arrange
        var data = new TestCreateStreamData(
            ProviderId.New(),
            "Company Salary",
            StreamCategory.Salary,
            "JPY",
            IsFixed: true,
            FixedPeriod: "Monthly");

        // Act
        var result = IncomeStream.Create(data);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsFixed.ShouldBeTrue();
        result.Value.FixedPeriod.ShouldBe("Monthly");
    }

    [Fact]
    public void Create_WithFixedIncomeWithoutPeriod_ShouldFail()
    {
        // Arrange
        var data = new TestCreateStreamData(
            ProviderId.New(),
            "Salary",
            StreamCategory.Salary,
            "USD",
            IsFixed: true,
            FixedPeriod: null);

        // Act
        var result = IncomeStream.Create(data);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        // Arrange
        var data = new TestCreateStreamData(
            ProviderId.New(),
            "",
            StreamCategory.Trading,
            "USD",
            IsFixed: false,
            FixedPeriod: null);

        // Act
        var result = IncomeStream.Create(data);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldRaiseStreamCreatedEvent()
    {
        // Arrange
        var data = new TestCreateStreamData(
            ProviderId.New(),
            "Test Stream",
            StreamCategory.Trading,
            "USD",
            IsFixed: false,
            FixedPeriod: null);

        // Act
        var result = IncomeStream.Create(data);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.DomainEvents.ShouldHaveSingleItem();
        result.Value.DomainEvents.First().ShouldBeOfType<StreamCreatedDomainEvent>();
    }

    #endregion

    #region RecordSnapshot Tests

    [Fact]
    public void RecordSnapshot_WithValidData_ShouldSucceed()
    {
        // Arrange
        var stream = CreateTestStream();
        var money = Money.Usd(5000m);
        var conversion = ExchangeConversion.NoConversion(money);
        var snapshotData = new TestRecordSnapshotData(
            DateOnly.FromDateTime(DateTime.Today),
            money,
            conversion);

        // Act
        var result = stream.RecordSnapshot(snapshotData);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        stream.Snapshots.Count.ShouldBe(1);
        stream.Snapshots.First().UsdAmount.ShouldBe(5000m);
    }

    [Fact]
    public void RecordSnapshot_SameDateTwice_ShouldUpdateExisting()
    {
        // Arrange
        var stream = CreateTestStream();
        var date = DateOnly.FromDateTime(DateTime.Today);

        var firstMoney = Money.Usd(5000m);
        var firstConversion = ExchangeConversion.NoConversion(firstMoney);
        stream.RecordSnapshot(new TestRecordSnapshotData(date, firstMoney, firstConversion));

        var secondMoney = Money.Usd(5200m);
        var secondConversion = ExchangeConversion.NoConversion(secondMoney);

        // Act
        var result = stream.RecordSnapshot(new TestRecordSnapshotData(date, secondMoney, secondConversion));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        stream.Snapshots.Count.ShouldBe(1);
        stream.Snapshots.First().UsdAmount.ShouldBe(5200m);
    }

    [Fact]
    public void RecordSnapshot_WhenDisabled_ShouldFail()
    {
        // Arrange
        var stream = CreateTestStream();
        stream.Disable();

        var money = Money.Usd(5000m);
        var conversion = ExchangeConversion.NoConversion(money);
        var snapshotData = new TestRecordSnapshotData(
            DateOnly.FromDateTime(DateTime.Today),
            money,
            conversion);

        // Act
        var result = stream.RecordSnapshot(snapshotData);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public void RecordSnapshot_ShouldRaiseSnapshotRecordedEvent()
    {
        // Arrange
        var stream = CreateTestStream();
        stream.ClearDomainEvents();

        var money = Money.Usd(5000m);
        var conversion = ExchangeConversion.NoConversion(money);
        var snapshotData = new TestRecordSnapshotData(
            DateOnly.FromDateTime(DateTime.Today),
            money,
            conversion);

        // Act
        stream.RecordSnapshot(snapshotData);

        // Assert
        stream.DomainEvents.ShouldHaveSingleItem();
        stream.DomainEvents.First().ShouldBeOfType<SnapshotRecordedDomainEvent>();
    }

    #endregion

    #region Query Tests

    [Fact]
    public void GetSnapshotByDate_ExistingDate_ShouldReturnSnapshot()
    {
        // Arrange
        var stream = CreateTestStream();
        var date = DateOnly.FromDateTime(DateTime.Today);
        RecordSnapshot(stream, date, 5000m);

        // Act
        var snapshot = stream.GetSnapshotByDate(date);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.UsdAmount.ShouldBe(5000m);
    }

    [Fact]
    public void GetSnapshotByDate_NonExistingDate_ShouldReturnNull()
    {
        // Arrange
        var stream = CreateTestStream();

        // Act
        var snapshot = stream.GetSnapshotByDate(DateOnly.FromDateTime(DateTime.Today));

        // Assert
        snapshot.ShouldBeNull();
    }

    [Fact]
    public void GetLatestSnapshot_WithMultipleSnapshots_ShouldReturnMostRecent()
    {
        // Arrange
        var stream = CreateTestStream();
        var today = DateOnly.FromDateTime(DateTime.Today);
        RecordSnapshot(stream, today.AddDays(-2), 4800m);
        RecordSnapshot(stream, today.AddDays(-1), 5000m);
        RecordSnapshot(stream, today, 5200m);

        // Act
        var latest = stream.GetLatestSnapshot();

        // Assert
        latest.ShouldNotBeNull();
        latest.Date.ShouldBe(today);
        latest.UsdAmount.ShouldBe(5200m);
    }

    [Fact]
    public void GetSnapshotsInRange_ShouldReturnOnlySnapshotsInRange()
    {
        // Arrange
        var stream = CreateTestStream();
        var today = DateOnly.FromDateTime(DateTime.Today);
        RecordSnapshot(stream, today.AddDays(-5), 4500m);
        RecordSnapshot(stream, today.AddDays(-3), 4800m);
        RecordSnapshot(stream, today.AddDays(-1), 5000m);
        RecordSnapshot(stream, today, 5200m);

        // Act
        var snapshots = stream.GetSnapshotsInRange(today.AddDays(-3), today.AddDays(-1)).ToList();

        // Assert
        snapshots.Count.ShouldBe(2);
        snapshots.First().UsdAmount.ShouldBe(4800m);
        snapshots.Last().UsdAmount.ShouldBe(5000m);
    }

    [Fact]
    public void GetTotalUsdInRange_ShouldSumAmounts()
    {
        // Arrange
        var stream = CreateTestStream();
        var today = DateOnly.FromDateTime(DateTime.Today);
        RecordSnapshot(stream, today.AddDays(-2), 100m);
        RecordSnapshot(stream, today.AddDays(-1), 150m);
        RecordSnapshot(stream, today, 200m);

        // Act
        var total = stream.GetTotalUsdInRange(today.AddDays(-2), today);

        // Assert
        total.ShouldBe(450m);
    }

    #endregion

    #region Sync Status Tests

    [Fact]
    public void MarkSyncFailed_ShouldRaiseEvent()
    {
        // Arrange
        var stream = CreateTestStream();
        stream.ClearDomainEvents();

        // Act
        stream.MarkSyncFailed("API timeout");

        // Assert
        stream.SyncStatus.State.ShouldBe(SyncState.Failed);
        stream.DomainEvents.ShouldHaveSingleItem();
        stream.DomainEvents.First().ShouldBeOfType<StreamSyncFailedDomainEvent>();
    }

    [Fact]
    public void DisableAndEnable_ShouldToggleState()
    {
        // Arrange
        var stream = CreateTestStream();

        // Act
        stream.Disable();
        var disabledState = stream.SyncStatus.State;
        stream.Enable();
        var enabledState = stream.SyncStatus.State;

        // Assert
        disabledState.ShouldBe(SyncState.Disabled);
        enabledState.ShouldBe(SyncState.Active);
    }

    #endregion

    #region Helpers

    private static IncomeStream CreateTestStream()
    {
        var data = new TestCreateStreamData(
            ProviderId.New(),
            "Test Stream",
            StreamCategory.Trading,
            "USD",
            IsFixed: false,
            FixedPeriod: null);

        return IncomeStream.Create(data).Value;
    }

    private static void RecordSnapshot(IncomeStream stream, DateOnly date, decimal usdAmount)
    {
        var money = Money.Usd(usdAmount);
        var conversion = ExchangeConversion.NoConversion(money);
        stream.RecordSnapshot(new TestRecordSnapshotData(date, money, conversion));
    }

    #endregion
}
