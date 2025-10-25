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
        var melodyPath = "path.mp3";

        // Act
        var profile = new AudioProfile(stationName, trackNumber, melodyPath);

        // Assert
        profile.Should().NotBeNull();
        profile.StationName.Should().Be(stationName);
        profile.TrackNumber.Should().Be(trackNumber);
        profile.MelodyFilePath.Should().Be(melodyPath);
    }

    [Fact]
    public void Constructor_NullStationName_ThrowsException()
    {
        // Arrange
        string? stationName = null;
        var trackNumber = "1";
        var melodyPath = "path.mp3";

        // Act
        Action act = () => new AudioProfile(stationName!, trackNumber, melodyPath);

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
        var melodyPath = "path.mp3";

        // Act
        Action act = () => new AudioProfile(stationName, trackNumber!, melodyPath);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(trackNumber));
    }

    [Fact]
    public void Constructor_NullMelodyPath_ThrowsException()
    {
        // Arrange
        var stationName = "館浜";
        var trackNumber = "1";
        string? melodyPath = null;

        // Act
        Action act = () => new AudioProfile(stationName, trackNumber, melodyPath!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("melodyFilePath");
    }

    [Fact]
    public void GetKey_ValidData_ReturnsFormattedKey()
    {
        // Arrange
        var profile = new AudioProfile("館浜", "1", "path.mp3");

        // Act
        var key = profile.GetKey();

        // Assert
        key.Should().Be("館浜_1");
    }

    [Fact]
    public void GetDoorCloseAnnouncementPath_Inbound_ReturnsUpPath()
    {
        // Arrange
        var upPath = "announce_up.mp3";
        var downPath = "announce_down.mp3";
        var profile = new AudioProfile("館浜", "1", "melody.mp3", downPath, upPath);

        // Act
        var result = profile.GetDoorCloseAnnouncementPath(true);

        // Assert
        result.Should().Be(upPath);
    }

    [Fact]
    public void GetDoorCloseAnnouncementPath_Outbound_ReturnsDownPath()
    {
        // Arrange
        var upPath = "announce_up.mp3";
        var downPath = "announce_down.mp3";
        var profile = new AudioProfile("館浜", "1", "melody.mp3", downPath, upPath);

        // Act
        var result = profile.GetDoorCloseAnnouncementPath(false);

        // Assert
        result.Should().Be(downPath);
    }
}