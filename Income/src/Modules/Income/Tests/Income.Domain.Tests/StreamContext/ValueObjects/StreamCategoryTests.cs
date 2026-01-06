namespace Income.Domain.Tests.StreamContext.ValueObjects;

public class StreamCategoryTests
{
    [Theory]
    [InlineData("Trading")]
    [InlineData("trading")]
    [InlineData("TRADING")]
    public void FromString_WithValidValue_ShouldReturnCategory(string value)
    {
        // Act
        var category = StreamCategory.FromString(value);

        // Assert
        category.ShouldNotBeNull();
        category.ShouldBe(StreamCategory.Trading);
    }

    [Fact]
    public void FromString_WithInvalidValue_ShouldReturnNull()
    {
        // Act
        var category = StreamCategory.FromString("InvalidCategory");

        // Assert
        category.ShouldBeNull();
    }

    [Fact]
    public void FromStringOrDefault_WithInvalidValue_ShouldReturnOther()
    {
        // Act
        var category = StreamCategory.FromStringOrDefault("Unknown");

        // Assert
        category.ShouldBe(StreamCategory.Other);
    }

    [Fact]
    public void GetAll_ShouldReturnAllCategories()
    {
        // Act
        var categories = StreamCategory.GetAll().ToList();

        // Assert
        categories.Count.ShouldBe(5);
        categories.ShouldContain(StreamCategory.Trading);
        categories.ShouldContain(StreamCategory.Referral);
        categories.ShouldContain(StreamCategory.Subscription);
        categories.ShouldContain(StreamCategory.Salary);
        categories.ShouldContain(StreamCategory.Other);
    }
}
