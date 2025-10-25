using FluentAssertions;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Domain.Models;

public class GameStateTests
{
    [Fact]
    public void IsPlaying_DrivingScreen_ReturnsTrue()
    {
        // Arrange
        var gameState = new GameState
        {
            Screen = GameScreen.Driving
        };

        // Act & Assert
        gameState.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void IsPlaying_ConductingScreen_ReturnsTrue()
    {
        // Arrange
        var gameState = new GameState
        {
            Screen = GameScreen.Conducting
        };

        // Act & Assert
        gameState.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void IsPlaying_MenuScreen_ReturnsFalse()
    {
        // Arrange
        var gameState = new GameState
        {
            Screen = GameScreen.Menu
        };

        // Act & Assert
        gameState.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void IsAtStation_True_WhenCircuitIdMatches()
    {
        // Arrange
        var gameState = new GameState
        {
            IsAtStation = true
        };

        // Act & Assert
        gameState.IsAtStation.Should().BeTrue();
    }

    [Fact]
    public void IsAtStation_False_WhenNotAtStation()
    {
        // Arrange
        var gameState = new GameState
        {
            IsAtStation = false
        };

        // Act & Assert
        gameState.IsAtStation.Should().BeFalse();
    }
}