using FluentAssertions;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Domain.Models;

public class AutoModeConfigTests
{
    [Fact]
    public void GetMarginForVehicle_50000Series_ReturnsSeries50000Margin()
    {
        // Arrange
        var config = new AutoModeConfig
        {
            StandardMargin = 8.5,
            Series50000Margin = 16.5
        };
        var trainState = new TrainState
        {
            VehicleTypes = new() { "50000", "50100" }
        };

        // Act
        var margin = config.GetMarginForVehicle(trainState);

        // Assert
        margin.Should().Be(16.5);
    }

    [Fact]
    public void GetMarginForVehicle_OtherSeries_ReturnsStandardMargin()
    {
        // Arrange
        var config = new AutoModeConfig
        {
            StandardMargin = 8.5,
            Series50000Margin = 16.5
        };
        var trainState = new TrainState
        {
            VehicleTypes = new() { "E233", "E235" }
        };

        // Act
        var margin = config.GetMarginForVehicle(trainState);

        // Assert
        margin.Should().Be(8.5);
    }
}