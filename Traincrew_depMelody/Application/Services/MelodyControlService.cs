using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Application.Services;

public class MelodyControlService : IMelodyControlService
{
    private readonly IAudioPlaybackService _audioPlayback;
    private readonly ITraincrewGameService _gameService;
    private readonly ILogger<MelodyControlService> _logger;
    private readonly object _stateLock = new();
    private readonly ITrackRepository _trackRepository;

    private MelodyState _currentState = new() { IsPlaying = false };

    public MelodyControlService(
        ITraincrewGameService gameService,
        ITrackRepository trackRepository,
        IAudioPlaybackService audioPlayback,
        ILogger<MelodyControlService> logger)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
        _audioPlayback = audioPlayback ?? throw new ArgumentNullException(nameof(audioPlayback));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ゲーム状態変化時のイベントハンドリング
        _gameService.GameStateChanged += OnGameStateChanged;
    }

    /// <summary>
    ///     メロディー再生を開始
    /// </summary>
    public async Task StartMelodyAsync()
    {
        lock (_stateLock)
        {
            if (_currentState.IsPlaying)
            {
                _logger.LogDebug("メロディーは既に再生中です");
                return;
            }
        }

        // ゲーム状態を取得（キャッシュから）
        var gameState = _gameService.GetCachedGameState();

        if (gameState.CurrentCircuitId.Count == 0)
        {
            _logger.LogWarning("軌道回路情報がありません");
            return;
        }

        var isAtStation = await _trackRepository.IsAnyCircuitAtStationAsync(gameState.CurrentCircuitId);
        if (!isAtStation)
        {
            _logger.LogWarning("駅に在線していません");
            return;
        }

        // 軌道回路から駅・番線を特定（複数ある場合は最初の軌道回路を使用）
        var track = await _trackRepository.FindTrackByCircuitIdAsync(gameState.CurrentCircuitId.First());
        if (track == null)
        {
            _logger.LogWarning("軌道回路ID '{GameStateCurrentCircuitId}' に対応する駅・番線が見つかりません", gameState.CurrentCircuitId.First());
            return;
        }

        _logger.LogInformation("メロディー再生開始: {TrackStationName} {TrackTrackNumber}番線", track.StationName, track.TrackNumber);

        // メロディー再生
        await _audioPlayback.PlayMelodyAsync(track);

        // 状態更新（キャッシュから取得）
        var currentGameState = _gameService.GetCachedGameState();
        lock (_stateLock)
        {
            _currentState = _currentState.With(
                true,
                currentTrack: track,
                startedAt: currentGameState.CurrentGameTime,
                doorCloseAnnouncementPlayed: false
            );
        }
    }

    /// <summary>
    ///     メロディーを停止し、ドア閉め案内を再生
    /// </summary>
    public async Task StopMelodyAsync()
    {
        TrackInfo? track;
        lock (_stateLock)
        {
            if (!_currentState.IsPlaying)
            {
                _logger.LogDebug("メロディーは再生されていません");
                return;
            }

            track = _currentState.CurrentTrack;
        }

        if (track == null)
        {
            _logger.LogWarning("現在の駅・番線情報がありません");
            return;
        }

        _logger.LogInformation("メロディー停止: {TrackStationName} {TrackTrackNumber}番線", track.StationName, track.TrackNumber);

        // メロディー停止
        _audioPlayback.StopMelody();

        // 状態更新
        lock (_stateLock)
        {
            _currentState = _currentState.With(false);
        }

        // ゲーム時刻で1秒待機
        var gameState = _gameService.GetCachedGameState();
        var startTime = gameState.CurrentGameTime;
        var targetTime = startTime.Add(TimeSpan.FromSeconds(1.0));

        while (true)
        {
            gameState = _gameService.GetCachedGameState();

            // ゲーム時刻が目標時刻に到達したら終了
            if (gameState.CurrentGameTime >= targetTime) break;

            await Task.Delay(16); // 16ms周期でチェック
        }

        // ドア閉め案内再生
        gameState = _gameService.GetCachedGameState();
        var isInbound = gameState.TrainState?.IsInbound() ?? false;

        _logger.LogInformation("ドア閉め案内再生: {TrackTrackNumber}番線 ({上り})", track.TrackNumber, isInbound ? "上り" : "下り");
        await _audioPlayback.PlayDoorCloseAnnouncementAsync(track, isInbound);

        // 案内再生済みフラグ
        lock (_stateLock)
        {
            _currentState = _currentState.With(doorCloseAnnouncementPlayed: true);
        }
    }

    /// <summary>
    ///     現在のメロディー状態を取得
    /// </summary>
    public MelodyState GetCurrentState()
    {
        lock (_stateLock)
        {
            return _currentState;
        }
    }

    /// <summary>
    ///     UI操作が有効かどうか
    /// </summary>
    public async Task<bool> IsUiEnabledAsync()
    {
        var gameState = _gameService.GetCachedGameState();
        var isAtStation = await _trackRepository.IsAnyCircuitAtStationAsync(gameState.CurrentCircuitId);
        return isAtStation && gameState.Screen == GameScreen.Playing;
    }

    /// <summary>
    ///     ゲーム状態が変化したときの処理
    /// </summary>
    private async void OnGameStateChanged(object? sender, GameState gameState)
    {
        // 一時停止状態の処理
        if (gameState.Screen == GameScreen.Pausing)
            _audioPlayback.PauseAll();
        else
            _audioPlayback.ResumeAll();

        // 駅から離れた場合、メロディーを停止
        var isAtStation = await _trackRepository.IsAnyCircuitAtStationAsync(gameState.CurrentCircuitId);
        if (!isAtStation)
            lock (_stateLock)
            {
                if (_currentState.IsPlaying)
                {
                    _logger.LogInformation("駅から離れたため、メロディーを停止します");
                    _audioPlayback.StopMelody();
                    _currentState = _currentState.With(false);
                }
            }
    }
}