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
    public void IsInbound_NonNumericSuffix_ReturnsFalse()
    {
        // Arrange
        var trainState = new TrainState { TrainNumber = "123A" };

        // Act
        var result = trainState.IsInbound();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsLimitedExpressType_50000Series_ReturnsTrue()
    {
        // Arrange
        var trainState = new TrainState
        {
            VehicleTypes = new List<string> { "50000", "50100" }
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
            VehicleTypes = new List<string> { "E233", "E235" }
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
            VehicleTypes = new List<string>()
        };

        // Act
        var result = trainState.IsLimitedExpressType();

        // Assert
        result.Should().BeFalse();
    }
}
