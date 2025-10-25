using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_depMelody.Application.Services;
using Traincrew_depMelody.Domain.Interfaces;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Application.Services;

public class MelodyControlServiceTests
{
    private readonly Mock<IAudioPlaybackService> _mockAudioPlayback;
    private readonly Mock<ITraincrewGameService> _mockGameService;
    private readonly Mock<ILogger<MelodyControlService>> _mockLogger;
    private readonly Mock<ITrackRepository> _mockTrackRepo;

    public MelodyControlServiceTests()
    {
        _mockGameService = new();
        _mockTrackRepo = new();
        _mockAudioPlayback = new();
        _mockLogger = new();
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var service = new MelodyControlService(
            _mockGameService.Object,
            _mockTrackRepo.Object,
            _mockAudioPlayback.Object,
            _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullGameService_ThrowsException()
    {
        // Act
        Action act = () => new MelodyControlService(
            null!,
            _mockTrackRepo.Object,
            _mockAudioPlayback.Object,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullTrackRepository_ThrowsException()
    {
        // Act
        Action act = () => new MelodyControlService(
            _mockGameService.Object,
            null!,
            _mockAudioPlayback.Object,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullAudioPlayback_ThrowsException()
    {
        // Act
        Action act = () => new MelodyControlService(
            _mockGameService.Object,
            _mockTrackRepo.Object,
            null!,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsException()
    {
        // Act
        Action act = () => new MelodyControlService(
            _mockGameService.Object,
            _mockTrackRepo.Object,
            _mockAudioPlayback.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task StartMelodyAsync_NotAtStation_DoesNothing()
    {
        // Arrange
        _mockGameService.Setup(x => x.GetCurrentGameStateAsync())
            .ReturnsAsync(new GameState
            {
                IsAtStation = false,
                CurrentCircuitId = null
            });

        var service = new MelodyControlService(
            _mockGameService.Object,
            _mockTrackRepo.Object,
            _mockAudioPlayback.Object,
            _mockLogger.Object);

        // Act
        await service.StartMelodyAsync();

        // Assert
        _mockAudioPlayback.Verify(x => x.PlayMelodyAsync(It.IsAny<TrackInfo>()), Times.Never);
    }

    [Fact]
    public void GetCurrentState_ReturnsCurrentState()
    {
        // Arrange
        var service = new MelodyControlService(
            _mockGameService.Object,
            _mockTrackRepo.Object,
            _mockAudioPlayback.Object,
            _mockLogger.Object);

        // Act
        var state = service.GetCurrentState();

        // Assert
        state.Should().NotBeNull();
        state.IsPlaying.Should().BeFalse();
    }
}