using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_depMelody.Application.Services;
using Traincrew_depMelody.Domain.Interfaces;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Application.Services;

public class AudioPlaybackServiceTests
{
    private readonly Mock<ILogger<AudioPlaybackService>> _mockLogger;
    private readonly Mock<IFFmpegService> _mockFFmpegService;
    private readonly Mock<IAudioProfileRepository> _mockAudioProfileRepository;
    private readonly AppConfiguration _config;
    private readonly Mock<IAudioPlayerService> _mockMelodyPlayer;
    private readonly Mock<IAudioPlayerService> _mockAnnouncementPlayer;

    public AudioPlaybackServiceTests()
    {
        _mockLogger = new Mock<ILogger<AudioPlaybackService>>();
        _mockFFmpegService = new Mock<IFFmpegService>();
        _mockAudioProfileRepository = new Mock<IAudioProfileRepository>();
        _config = new AppConfiguration
        {
            DefaultMelodyDownPath = "C:\\Test\\Audio\\default_down.mp3",
            DefaultMelodyUpPath = "C:\\Test\\Audio\\default_up.mp3"
        };
        _mockMelodyPlayer = new Mock<IAudioPlayerService>();
        _mockAnnouncementPlayer = new Mock<IAudioPlayerService>();
    }

    /// <summary>
    /// ポーズ前にメロディーのみが再生中の場合、リジューム時にメロディーのみが再開される
    /// </summary>
    [Fact]
    public void ResumeAll_WhenOnlyMelodyWasPlaying_ShouldResumeOnlyMelody()
    {
        // Arrange
        var service = CreateServiceWithMockedPlayers();

        // メロディーは再生中、アナウンスは停止中
        _mockMelodyPlayer.Setup(m => m.IsPlaying).Returns(true);
        _mockAnnouncementPlayer.Setup(m => m.IsPlaying).Returns(false);

        // Act
        service.PauseAll();
        service.ResumeAll();

        // Assert
        _mockMelodyPlayer.Verify(m => m.Resume(), Times.Once, "メロディーは再生中だったため再開されるべき");
        _mockAnnouncementPlayer.Verify(m => m.Resume(), Times.Never, "アナウンスは停止中だったため再開されないべき");
    }

    /// <summary>
    /// ポーズ前にアナウンスのみが再生中の場合、リジューム時にアナウンスのみが再開される
    /// </summary>
    [Fact]
    public void ResumeAll_WhenOnlyAnnouncementWasPlaying_ShouldResumeOnlyAnnouncement()
    {
        // Arrange
        var service = CreateServiceWithMockedPlayers();

        // メロディーは停止中、アナウンスは再生中
        _mockMelodyPlayer.Setup(m => m.IsPlaying).Returns(false);
        _mockAnnouncementPlayer.Setup(m => m.IsPlaying).Returns(true);

        // Act
        service.PauseAll();
        service.ResumeAll();

        // Assert
        _mockMelodyPlayer.Verify(m => m.Resume(), Times.Never, "メロディーは停止中だったため再開されないべき");
        _mockAnnouncementPlayer.Verify(m => m.Resume(), Times.Once, "アナウンスは再生中だったため再開されるべき");
    }

    /// <summary>
    /// ポーズ前に両方とも再生中の場合、リジューム時に両方とも再開される
    /// </summary>
    [Fact]
    public void ResumeAll_WhenBothWerePlaying_ShouldResumeBoth()
    {
        // Arrange
        var service = CreateServiceWithMockedPlayers();

        // 両方とも再生中
        _mockMelodyPlayer.Setup(m => m.IsPlaying).Returns(true);
        _mockAnnouncementPlayer.Setup(m => m.IsPlaying).Returns(true);

        // Act
        service.PauseAll();
        service.ResumeAll();

        // Assert
        _mockMelodyPlayer.Verify(m => m.Resume(), Times.Once, "メロディーは再生中だったため再開されるべき");
        _mockAnnouncementPlayer.Verify(m => m.Resume(), Times.Once, "アナウンスは再生中だったため再開されるべき");
    }

    /// <summary>
    /// ポーズ前に何も再生していない場合、リジューム時に何も再開されない
    /// </summary>
    [Fact]
    public void ResumeAll_WhenNothingWasPlaying_ShouldResumeNothing()
    {
        // Arrange
        var service = CreateServiceWithMockedPlayers();

        // 両方とも停止中
        _mockMelodyPlayer.Setup(m => m.IsPlaying).Returns(false);
        _mockAnnouncementPlayer.Setup(m => m.IsPlaying).Returns(false);

        // Act
        service.PauseAll();
        service.ResumeAll();

        // Assert
        _mockMelodyPlayer.Verify(m => m.Resume(), Times.Never, "メロディーは停止中だったため再開されないべき");
        _mockAnnouncementPlayer.Verify(m => m.Resume(), Times.Never, "アナウンスは停止中だったため再開されないべき");
    }

    /// <summary>
    /// 複数回のポーズ・リジュームサイクルで状態が正しく管理される
    /// </summary>
    [Fact]
    public void ResumeAll_MultiplePauseResumeCycles_ShouldTrackStateCorrectly()
    {
        // Arrange
        var service = CreateServiceWithMockedPlayers();

        // 1回目: メロディーのみ再生中
        _mockMelodyPlayer.Setup(m => m.IsPlaying).Returns(true);
        _mockAnnouncementPlayer.Setup(m => m.IsPlaying).Returns(false);

        // Act & Assert - 1回目のサイクル
        service.PauseAll();
        service.ResumeAll();

        _mockMelodyPlayer.Verify(m => m.Resume(), Times.Once);
        _mockAnnouncementPlayer.Verify(m => m.Resume(), Times.Never);

        // 2回目: アナウンスのみ再生中
        _mockMelodyPlayer.Setup(m => m.IsPlaying).Returns(false);
        _mockAnnouncementPlayer.Setup(m => m.IsPlaying).Returns(true);

        // Act & Assert - 2回目のサイクル
        service.PauseAll();
        service.ResumeAll();

        _mockMelodyPlayer.Verify(m => m.Resume(), Times.Once, "1回目のみ再開されるべき");
        _mockAnnouncementPlayer.Verify(m => m.Resume(), Times.Once, "2回目のみ再開されるべき");
    }

    /// <summary>
    /// ポーズせずにリジュームを呼んだ場合、何も再開されない
    /// </summary>
    [Fact]
    public void ResumeAll_WithoutPause_ShouldResumeNothing()
    {
        // Arrange
        var service = CreateServiceWithMockedPlayers();

        // メロディーとアナウンスが再生中の設定をするが、PauseAllを呼ばない
        _mockMelodyPlayer.Setup(m => m.IsPlaying).Returns(true);
        _mockAnnouncementPlayer.Setup(m => m.IsPlaying).Returns(true);

        // Act
        service.ResumeAll(); // PauseAllを呼ばずにResumeAllを呼ぶ

        // Assert
        _mockMelodyPlayer.Verify(m => m.Resume(), Times.Never, "PauseAllを呼んでいないため再開されないべき");
        _mockAnnouncementPlayer.Verify(m => m.Resume(), Times.Never, "PauseAllを呼んでいないため再開されないべき");
    }

    /// <summary>
    /// PauseAllは常に両方のプレイヤーをポーズする
    /// </summary>
    [Fact]
    public void PauseAll_ShouldAlwaysPauseBothPlayers()
    {
        // Arrange
        var service = CreateServiceWithMockedPlayers();

        _mockMelodyPlayer.Setup(m => m.IsPlaying).Returns(true);
        _mockAnnouncementPlayer.Setup(m => m.IsPlaying).Returns(false);

        // Act
        service.PauseAll();

        // Assert
        _mockMelodyPlayer.Verify(m => m.Pause(), Times.Once, "再生中の状態に関わらずポーズされるべき");
        _mockAnnouncementPlayer.Verify(m => m.Pause(), Times.Once, "再生中でなくてもポーズされるべき");
    }

    /// <summary>
    /// テスト用にモックされたプレイヤーを持つサービスを作成
    /// </summary>
    private AudioPlaybackService CreateServiceWithMockedPlayers()
    {
        // リフレクションを使用してプライベートフィールドにモックを注入
        var service = new AudioPlaybackService(
            _mockLogger.Object,
            _mockFFmpegService.Object,
            _mockAudioProfileRepository.Object,
            _config);

        var type = service.GetType();
        var melodyPlayerField = type.GetField("_melodyPlayer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var announcementPlayerField = type.GetField("_announcementPlayer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        melodyPlayerField?.SetValue(service, _mockMelodyPlayer.Object);
        announcementPlayerField?.SetValue(service, _mockAnnouncementPlayer.Object);

        return service;
    }
}
