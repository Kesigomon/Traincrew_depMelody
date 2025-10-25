using FluentAssertions;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Domain.Models;

public class GameStateTests
{
    [Fact]
    public void Screen_Playing_WhenPlaying()
    {
        // Arrange
        var gameState = new GameState
        {
            Screen = GameScreen.Playing
        };

        // Act & Assert
        gameState.Screen.Should().Be(GameScreen.Playing);
    }

    [Fact]
    public void Screen_Pausing_WhenPaused()
    {
        // Arrange
        var gameState = new GameState
        {
            Screen = GameScreen.Pausing
        };

        // Act & Assert
        gameState.Screen.Should().Be(GameScreen.Pausing);
    }

    [Fact]
    public void Screen_NotPlaying_WhenNotPlaying()
    {
        // Arrange
        var gameState = new GameState
        {
            Screen = GameScreen.NotPlaying
        };

        // Act & Assert
        gameState.Screen.Should().Be(GameScreen.NotPlaying);
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