using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_depMelody.Domain.Models;
using Traincrew_depMelody.Infrastructure.Repositories;

namespace Traincrew_depMelody.Tests.Infrastructure.Repositories;

public class TrackRepositoryTests : IDisposable
{
    private readonly AppConfiguration _config;
    private readonly Mock<ILogger<TrackRepository>> _mockLogger;
    private readonly string _testCsvPath;

    public TrackRepositoryTests()
    {
        // テスト用のCSVファイルパスを設定
        _testCsvPath = Path.Combine(AppContext.BaseDirectory, "TestData", "stations.csv");

        _config = new()
        {
            StationsCsvPath = _testCsvPath
        };

        _mockLogger = new();
    }

    public void Dispose()
    {
        // クリーンアップ処理(必要に応じて)
    }

    [Fact]
    public async Task FindTrackByCircuitIdAsync_ExistingCircuit_ReturnsTrack()
    {
        // Arrange
        var repository = new TrackRepository(_config, _mockLogger.Object);

        // Act
        var track = await repository.FindTrackByCircuitIdAsync(new[] { "TC_TATEHAMA_01_1", "TC_TATEHAMA_01_2" }, null);

        // Assert
        track.Should().NotBeNull();
        track!.StationName.Should().Be("館浜");
        track.TrackNumber.Should().Be("1");
    }

    [Fact]
    public async Task FindTrackByCircuitIdAsync_NonExistingCircuit_ReturnsNull()
    {
        // Arrange
        var repository = new TrackRepository(_config, _mockLogger.Object);

        // Act
        var track = await repository.FindTrackByCircuitIdAsync(new[] { "TC_NONEXISTENT" }, null);

        // Assert
        track.Should().BeNull();
    }

    [Fact]
    public async Task FindTrackByCircuitIdAsync_Tatehama2_TrainNumberEndsWithA_ReturnsTrack2()
    {
        // Arrange
        var repository = new TrackRepository(_config, _mockLogger.Object);

        // Act
        var track = await repository.FindTrackByCircuitIdAsync(new[] { "TC_TATEHAMA_02_1", "TC_TATEHAMA_02_2", "TC_TATEHAMA_02_3" }, "1206A");

        // Assert
        track.Should().NotBeNull();
        track!.TrackNumber.Should().Be("2");
    }

    [Fact]
    public async Task FindTrackByCircuitIdAsync_Tatehama2_TrainNumberEndsWithDigit_ReturnsTrack3()
    {
        // Arrange
        var repository = new TrackRepository(_config, _mockLogger.Object);

        // Act
        var track = await repository.FindTrackByCircuitIdAsync(new[] { "TC_TATEHAMA_02_1", "TC_TATEHAMA_02_2", "TC_TATEHAMA_02_3" }, "1206");

        // Assert
        track.Should().NotBeNull();
        track!.TrackNumber.Should().Be("3");
    }

    [Fact]
    public async Task FindTrackByCircuitIdAsync_Tatehama2_TrainNumberNull_ReturnsTrack3()
    {
        // Arrange
        var repository = new TrackRepository(_config, _mockLogger.Object);

        // Act
        var track = await repository.FindTrackByCircuitIdAsync(new[] { "TC_TATEHAMA_02_1", "TC_TATEHAMA_02_2", "TC_TATEHAMA_02_3" }, null);

        // Assert
        track.Should().NotBeNull();
        track!.TrackNumber.Should().Be("3");
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsException()
    {
        // Act
        Action act = () => new TrackRepository(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsException()
    {
        // Act
        Action act = () => new TrackRepository(_config, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}