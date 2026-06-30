using FluentAssertions;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Domain.Models;

public class TrainStateTests
{
    [Fact]
    public void IsStopped_SpeedZero_ReturnsTrue()
    {
        // Arrange
        var trainState = new TrainState { Speed = 0.0 };

        // Act & Assert
        trainState.IsStopped.Should().BeTrue();
    }

    [Fact]
    public void IsStopped_SpeedNearZero_ReturnsTrue()
    {
        // Arrange
        var trainState = new TrainState { Speed = 0.05 };

        // Act & Assert
        trainState.IsStopped.Should().BeTrue();
    }

    [Fact]
    public void IsStopped_SpeedAboveThreshold_ReturnsFalse()
    {
        // Arrange
        var trainState = new TrainState { Speed = 0.2 };

        // Act & Assert
        trainState.IsStopped.Should().BeFalse();
    }

    [Fact]
    public void IsInbound_EvenTrainNumber_ReturnsTrue()
    {
        // Arrange
        var trainState = new TrainState { TrainNumber = "1234" };

        // Act
        var result = trainState.IsInbound();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInbound_OddTrainNumber_ReturnsFalse()
    {
        // Arrange
        var trainState = new TrainState { TrainNumber = "1235" };

        // Act
        var result = trainState.IsInbound();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInbound_NullTrainNumber_ReturnsFalse()
    {
        // Arrange
        var trainState = new TrainState { TrainNumber = null };

        // Act
        var result = trainState.IsInbound();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInbound_SuffixLetterUsesLastDigit_Odd()
    {
        // Arrange: 末尾英字でも最後の数字 `3` が奇数なので false
        var trainState = new TrainState { TrainNumber = "123A" };

        // Act
        var result = trainState.IsInbound();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("624A", true)]   // 末尾英字、最後の数字 4 が偶数 → 上り
    [InlineData("681B", false)]   // 末尾英字、最後の数字 1 が奇数 → 下り
    [InlineData("1184C", true)]    // 末尾英字、最後の数字 4 が偶数 → 上り
    [InlineData("1A2B", true)]    // 数字+英字が中間で混在、末尾側の数字 2 が偶数 → 上り
    [InlineData("ABC", false)]    // 数字なし → false
    [InlineData("", false)]       // 空文字 → false
    public void IsInbound_SuffixLetter_UsesLastDigitForParity(string trainNumber, bool expected)
    {
        // Arrange
        var trainState = new TrainState { TrainNumber = trainNumber };

        // Act
        var result = trainState.IsInbound();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsLimitedExpressType_50000Series_ReturnsTrue()
    {
        // Arrange
        var trainState = new TrainState
        {
            VehicleTypes = new() { "50000", "50100" }
        };

        // Act
        var result = trainState.IsLimitedExpressType();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLimitedExpressType_OtherSeries_ReturnsFalse()
    {
        // Arrange
        var trainState = new TrainState
        {
            VehicleTypes = new() { "E233", "E235" }
        };

        // Act
        var result = trainState.IsLimitedExpressType();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsLimitedExpressType_EmptyVehicleTypes_ReturnsFalse()
    {
        // Arrange
        var trainState = new TrainState
        {
            VehicleTypes = new()
        };

        // Act
        var result = trainState.IsLimitedExpressType();

        // Assert
        result.Should().BeFalse();
    }
}