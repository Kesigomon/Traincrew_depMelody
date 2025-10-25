using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_depMelody.Domain.Models;
using Traincrew_depMelody.Infrastructure.Repositories;

namespace Traincrew_depMelody.Tests.Infrastructure.Repositories;

public class AudioProfileRepositoryTests : IDisposable
{
    private readonly AppConfiguration _config;
    private readonly Mock<ILogger<AudioProfileRepository>> _mockLogger;

    public AudioProfileRepositoryTests()
    {
        // テスト用のCSVファイルパスを設定
        var testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");

        _config = new()
        {
            ProfilesDirectory = testDataDir,
            CurrentProfileName = "AudioProfile"
        };

        _mockLogger = new();
    }

    public void Dispose()
    {
        // クリーンアップ処理(必要に応じて)
    }

    [Fact]
    public async Task GetAllProfilesAsync_ValidCsv_ReturnsAllProfiles()
    {
        // Arrange
        var repository = new AudioProfileRepository(_config, _mockLogger.Object);

        // Act
        var profiles = await repository.GetAllProfilesAsync();

        // Assert
        profiles.Should().NotBeNull();
        profiles.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task FindProfileAsync_ExistingProfile_ReturnsProfile()
    {
        // Arrange
        var repository = new AudioProfileRepository(_config, _mockLogger.Object);

        // Act
        var profile = await repository.FindProfileAsync("館浜", "1");

        // Assert
        profile.Should().NotBeNull();
        profile!.StationName.Should().Be("館浜");
        profile.TrackNumber.Should().Be("1");
        profile.MelodyFilePath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FindProfileAsync_NonExistingProfile_ReturnsNull()
    {
        // Arrange
        var repository = new AudioProfileRepository(_config, _mockLogger.Object);

        // Act
        var profile = await repository.FindProfileAsync("存在しない駅", "1");

        // Assert
        profile.Should().BeNull();
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsException()
    {
        // Act
        Action act = () => new AudioProfileRepository(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsException()
    {
        // Act
        Action act = () => new AudioProfileRepository(_config, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}