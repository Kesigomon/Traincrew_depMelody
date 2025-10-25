using FluentAssertions;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Domain.Models;

public class MelodyStateTests
{
    [Fact]
    public void With_UpdateIsPlaying_CreatesNewState()
    {
        // Arrange
        var originalState = new MelodyState { IsPlaying = false };

        // Act
        var newState = originalState.With(true);

        // Assert
        newState.IsPlaying.Should().BeTrue();
        newState.Should().NotBeSameAs(originalState);
    }

    [Fact]
    public void With_UpdateMultipleProperties_PreservesOthers()
    {
        // Arrange
        var trackInfo = new TrackInfo("館浜", "1", new() { "TC_001" });
        var originalState = new MelodyState
        {
            IsPlaying = false,
            CurrentMelodyPath = "original.mp3",
            DoorCloseAnnouncementPlayed = false
        };

        // Act
        var newState = originalState.With(
            true,
            currentTrack: trackInfo);

        // Assert
        newState.IsPlaying.Should().BeTrue();
        newState.CurrentTrack.Should().Be(trackInfo);
        newState.CurrentMelodyPath.Should().Be("original.mp3");
        newState.DoorCloseAnnouncementPlayed.Should().BeFalse();
    }

    [Fact]
    public void With_NoParameters_ReturnsSameValues()
    {
        // Arrange
        var trackInfo = new TrackInfo("館浜", "1", new() { "TC_001" });
        var originalState = new MelodyState
        {
            IsPlaying = true,
            CurrentMelodyPath = "melody.mp3",
            CurrentTrack = trackInfo,
            StartedAt = TimeSpan.FromSeconds(100),
            DoorCloseAnnouncementPlayed = true
        };

        // Act
        var newState = originalState.With();

        // Assert
        newState.IsPlaying.Should().Be(originalState.IsPlaying);
        newState.CurrentMelodyPath.Should().Be(originalState.CurrentMelodyPath);
        newState.CurrentTrack.Should().Be(originalState.CurrentTrack);
        newState.StartedAt.Should().Be(originalState.StartedAt);
        newState.DoorCloseAnnouncementPlayed.Should().Be(originalState.DoorCloseAnnouncementPlayed);
        newState.Should().NotBeSameAs(originalState);
    }
}