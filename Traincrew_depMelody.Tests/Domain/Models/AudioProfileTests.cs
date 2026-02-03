using FluentAssertions;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Domain.Models;

public class AudioProfileTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesInstance()
    {
        // Arrange
        var stationName = "館浜";
        var trackNumber = "1";
        var melodyDownPath = "down.mp3";
        var melodyUpPath = "up.mp3";

        // Act
        var profile = new AudioProfile(stationName, trackNumber, melodyDownPath, melodyUpPath);

        // Assert
        profile.Should().NotBeNull();
        profile.StationName.Should().Be(stationName);
        profile.TrackNumber.Should().Be(trackNumber);
        profile.MelodyDownFilePath.Should().Be(melodyDownPath);
        profile.MelodyUpFilePath.Should().Be(melodyUpPath);
    }

    [Fact]
    public void Constructor_NullStationName_ThrowsException()
    {
        // Arrange
        string? stationName = null;
        var trackNumber = "1";

        // Act
        Action act = () => new AudioProfile(stationName!, trackNumber, "down.mp3", "up.mp3");

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

        // Act
        Action act = () => new AudioProfile(stationName, trackNumber!, "down.mp3", "up.mp3");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(trackNumber));
    }

    [Fact]
    public void Constructor_NullMelodyDownFilePath_ThrowsException()
    {
        // Arrange
        var stationName = "館浜";
        var trackNumber = "1";
        string? melodyDownPath = null;

        // Act
        Action act = () => new AudioProfile(stationName, trackNumber, melodyDownPath!, "up.mp3");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("melodyDownFilePath");
    }

    [Fact]
    public void GetKey_ValidData_ReturnsFormattedKey()
    {
        // Arrange
        var profile = new AudioProfile("館浜", "1", "down.mp3", "up.mp3");

        // Act
        var key = profile.GetKey();

        // Assert
        key.Should().Be("館浜_1");
    }

    [Fact]
    public void GetDoorCloseAnnouncementPath_Inbound_ReturnsUpPath()
    {
        // Arrange
        var announceUpPath = "announce_up.mp3";
        var announceDownPath = "announce_down.mp3";
        var profile = new AudioProfile("館浜", "1", "melody_down.mp3", "melody_up.mp3", announceDownPath, announceUpPath);

        // Act
        var result = profile.GetDoorCloseAnnouncementPath(true);

        // Assert
        result.Should().Be(announceUpPath);
    }

    [Fact]
    public void GetDoorCloseAnnouncementPath_Outbound_ReturnsDownPath()
    {
        // Arrange
        var announceUpPath = "announce_up.mp3";
        var announceDownPath = "announce_down.mp3";
        var profile = new AudioProfile("館浜", "1", "melody_down.mp3", "melody_up.mp3", announceDownPath, announceUpPath);

        // Act
        var result = profile.GetDoorCloseAnnouncementPath(false);

        // Assert
        result.Should().Be(announceDownPath);
    }
}