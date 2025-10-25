using FluentAssertions;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Domain.Models;

public class TrackInfoTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesInstance()
    {
        // Arrange
        var stationName = "館浜";
        var trackNumber = "1";
        var circuitIds = new List<string> { "TC_001" };

        // Act
        var trackInfo = new TrackInfo(stationName, trackNumber, circuitIds);

        // Assert
        trackInfo.Should().NotBeNull();
        trackInfo.StationName.Should().Be(stationName);
        trackInfo.TrackNumber.Should().Be(trackNumber);
        trackInfo.CircuitIds.Should().BeEquivalentTo(circuitIds);
    }

    [Fact]
    public void Constructor_NullStationName_ThrowsException()
    {
        // Arrange
        string? stationName = null;
        var trackNumber = "1";
        var circuitIds = new List<string> { "TC_001" };

        // Act
        Action act = () => new TrackInfo(stationName!, trackNumber, circuitIds);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(stationName));
    }

    [Fact]
    public void Constructor_NullTrackNumber_ThrowsException()
    {
        // Arrange
        var stationName = "館浜";
        string? trackNumber = null;
        var circuitIds = new List<string> { "TC_001" };

        // Act
        Action act = () => new TrackInfo(stationName, trackNumber!, circuitIds);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(trackNumber));
    }

    [Fact]
    public void Constructor_NullCircuitIds_ThrowsException()
    {
        // Arrange
        var stationName = "館浜";
        var trackNumber = "1";
        List<string>? circuitIds = null;

        // Act
        Action act = () => new TrackInfo(stationName, trackNumber, circuitIds!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(circuitIds));
    }

    [Fact]
    public void ContainsCircuit_ExistingCircuit_ReturnsTrue()
    {
        // Arrange
        var trackInfo = new TrackInfo("館浜", "1", new List<string> { "TC_001", "TC_002" });

        // Act
        var result = trackInfo.ContainsCircuit("TC_001");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsCircuit_NonExistingCircuit_ReturnsFalse()
    {
        // Arrange
        var trackInfo = new TrackInfo("館浜", "1", new List<string> { "TC_001", "TC_002" });

        // Act
        var result = trackInfo.ContainsCircuit("TC_999");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetKey_ValidData_ReturnsFormattedKey()
    {
        // Arrange
        var trackInfo = new TrackInfo("館浜", "1", new List<string> { "TC_001" });

        // Act
        var key = trackInfo.GetKey();

        // Assert
        key.Should().Be("館浜_1");
    }
}
