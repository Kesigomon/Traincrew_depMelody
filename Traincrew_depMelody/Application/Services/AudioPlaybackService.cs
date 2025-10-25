using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Traincrew_depMelody.Domain.Interfaces;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;
using Traincrew_depMelody.Infrastructure.AudioServices;

namespace Traincrew_depMelody.Application.Services;

public class AudioPlaybackService : IAudioPlaybackService
{
    private readonly IAudioPlayerService _announcementPlayer;
    private readonly IAudioProfileRepository _audioProfileRepository;
    private readonly AppConfiguration _config;
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioPlaybackService> _logger;
    private readonly IAudioPlayerService _melodyPlayer;

    public AudioPlaybackService(
        ILogger<AudioPlaybackService> logger,
        IFFmpegService ffmpegService,
        IAudioProfileRepository audioProfileRepository,
        AppConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ffmpegService = ffmpegService ?? throw new ArgumentNullException(nameof(ffmpegService));
        _audioProfileRepository =
            audioProfileRepository ?? throw new ArgumentNullException(nameof(audioProfileRepository));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // 2つのMediaPlayerインスタンスを作成(メロディー用と案内用)
        _melodyPlayer = new MediaPlayerService(
            NullLogger<MediaPlayerService>
                .Instance);
        _announcementPlayer = new MediaPlayerService(
            NullLogger<MediaPlayerService>
                .Instance);
    }

    /// <summary>
    ///     メロディーを再生(ループ)
    /// </summary>
    public async Task PlayMelodyAsync(TrackInfo track)
    {
        // 音声プロファイルから実際のファイルパスを取得
        var profile = await _audioProfileRepository.FindProfileAsync(track.StationName, track.TrackNumber);

        if (profile == null)
        {
            _logger.LogWarning("音声プロファイルが見つかりません: {TrackStationName} {TrackTrackNumber}番線、デフォルトを使用", track.StationName, track.TrackNumber);
            await _melodyPlayer.PlayLoopAsync(GetDefaultMelodyPath());
            return;
        }

        var melodyPath = profile.MelodyFilePath;

        if (!File.Exists(melodyPath))
        {
            _logger.LogWarning("メロディーファイルが見つかりません: {MelodyPath}、デフォルトを使用", melodyPath);
            melodyPath = GetDefaultMelodyPath();

            if (!File.Exists(melodyPath))
            {
                _logger.LogError("デフォルトメロディーも見つかりません");
                throw new FileNotFoundException("メロディーファイルが見つかりません", melodyPath);
            }
        }

        await _melodyPlayer.PlayLoopAsync(melodyPath);
    }

    /// <summary>
    ///     メロディーを停止
    /// </summary>
    public void StopMelody()
    {
        _melodyPlayer.Stop();
    }

    /// <summary>
    ///     ドア閉め案内を再生(1回)
    /// </summary>
    public async Task PlayDoorCloseAnnouncementAsync(TrackInfo track, bool isInbound)
    {
        // 音声プロファイルから実際のファイルパスを取得
        var profile = await _audioProfileRepository.FindProfileAsync(track.StationName, track.TrackNumber);

        if (profile == null)
        {
            _logger.LogWarning("音声プロファイルが見つかりません: {TrackStationName} {TrackTrackNumber}番線", track.StationName, track.TrackNumber);
            return;
        }

        var announcementPath = profile.GetDoorCloseAnnouncementPath(isInbound);

        if (string.IsNullOrEmpty(announcementPath))
        {
            _logger.LogWarning(
                "ドア閉め案内ファイルパスが設定されていません: {TrackStationName} {TrackTrackNumber}番線 ({上り})", track.StationName, track.TrackNumber, isInbound ? "上り" : "下り");
            return;
        }

        if (!File.Exists(announcementPath))
        {
            _logger.LogWarning("ドア閉め案内ファイルが見つかりません: {AnnouncementPath}", announcementPath);
            return;
        }

        await _announcementPlayer.PlayOnceAsync(announcementPath);
    }

    /// <summary>
    ///     全ての音声を一時停止
    /// </summary>
    public void PauseAll()
    {
        _melodyPlayer.Pause();
        _announcementPlayer.Pause();
    }

    /// <summary>
    ///     一時停止から再開
    /// </summary>
    public void ResumeAll()
    {
        _melodyPlayer.Resume();
        _announcementPlayer.Resume();
    }

    /// <summary>
    ///     メロディーの長さを取得(秒)
    /// </summary>
    public async Task<double> GetMelodyDurationAsync(TrackInfo track)
    {
        // 音声プロファイルから実際のファイルパスを取得
        var profile = await _audioProfileRepository.FindProfileAsync(track.StationName, track.TrackNumber);

        string melodyPath;
        if (profile == null || !File.Exists(profile.MelodyFilePath))
            melodyPath = GetDefaultMelodyPath();
        else
            melodyPath = profile.MelodyFilePath;

        var duration = await _ffmpegService.GetAudioDurationAsync(melodyPath);
        return duration ?? 30.0; // デフォルト30秒
    }

    /// <summary>
    ///     デフォルトメロディーパスを取得
    /// </summary>
    private string GetDefaultMelodyPath()
    {
        return Path.Combine(_config.AudioBaseDirectory, _config.DefaultMelodyFileName);
    }
}