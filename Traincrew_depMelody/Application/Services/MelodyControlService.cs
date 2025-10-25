using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Application.Services;

public class MelodyControlService : IMelodyControlService
{
    private readonly ITraincrewGameService _gameService;
    private readonly ITrackRepository _trackRepository;
    private readonly IAudioPlaybackService _audioPlayback;
    private readonly ILogger<MelodyControlService> _logger;

    private MelodyState _currentState = new MelodyState { IsPlaying = false };
    private readonly object _stateLock = new object();

    public event EventHandler<MelodyState>? StateChanged;

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
    /// メロディー再生を開始
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

        // ゲーム状態を取得
        var gameState = await _gameService.GetCurrentGameStateAsync();

        if (!gameState.IsAtStation || gameState.CurrentCircuitId == null)
        {
            _logger.LogWarning("駅に在線していません");
            return;
        }

        // 軌道回路から駅・番線を特定
        var track = await _trackRepository.FindTrackByCircuitIdAsync(gameState.CurrentCircuitId);
        if (track == null)
        {
            _logger.LogWarning($"軌道回路ID '{gameState.CurrentCircuitId}' に対応する駅・番線が見つかりません");
            return;
        }

        _logger.LogInformation($"メロディー再生開始: {track.StationName} {track.TrackNumber}番線");

        // メロディー再生
        await _audioPlayback.PlayMelodyAsync(track);

        // 状態更新
        var currentGameState = await _gameService.GetCurrentGameStateAsync();
        lock (_stateLock)
        {
            _currentState = _currentState.With(
                isPlaying: true,
                currentTrack: track,
                startedAt: currentGameState.CurrentGameTime,
                doorCloseAnnouncementPlayed: false
            );
        }

        StateChanged?.Invoke(this, _currentState);
    }

    /// <summary>
    /// メロディーを停止し、ドア閉め案内を再生
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

        _logger.LogInformation($"メロディー停止: {track.StationName} {track.TrackNumber}番線");

        // メロディー停止
        _audioPlayback.StopMelody();

        // 状態更新
        lock (_stateLock)
        {
            _currentState = _currentState.With(isPlaying: false);
        }

        StateChanged?.Invoke(this, _currentState);

        // ゲーム時刻で1秒待機
        var gameState = await _gameService.GetCurrentGameStateAsync();
        TimeSpan startTime = gameState.CurrentGameTime;
        TimeSpan targetTime = startTime.Add(TimeSpan.FromSeconds(1.0));

        while (true)
        {
            gameState = await _gameService.GetCurrentGameStateAsync();

            // ゲーム時刻が目標時刻に到達したら終了
            if (gameState.CurrentGameTime >= targetTime)
            {
                break;
            }

            await Task.Delay(16); // 16ms周期でチェック
        }

        // ドア閉め案内再生
        gameState = await _gameService.GetCurrentGameStateAsync();
        bool isInbound = gameState.TrainState?.IsInbound() ?? false;

        _logger.LogInformation($"ドア閉め案内再生: {track.TrackNumber}番線 ({(isInbound ? "上り" : "下り")})");
        await _audioPlayback.PlayDoorCloseAnnouncementAsync(track, isInbound);

        // 案内再生済みフラグ
        lock (_stateLock)
        {
            _currentState = _currentState.With(doorCloseAnnouncementPlayed: true);
        }

        StateChanged?.Invoke(this, _currentState);
    }

    /// <summary>
    /// 現在のメロディー状態を取得
    /// </summary>
    public MelodyState GetCurrentState()
    {
        lock (_stateLock)
        {
            return _currentState;
        }
    }

    /// <summary>
    /// UI操作が有効かどうか
    /// </summary>
    public async Task<bool> IsUiEnabledAsync()
    {
        var gameState = await _gameService.GetCurrentGameStateAsync();
        return gameState.IsAtStation && !gameState.IsPaused;
    }

    /// <summary>
    /// ゲーム状態が変化したときの処理
    /// </summary>
    private void OnGameStateChanged(object? sender, GameState gameState)
    {
        // 一時停止状態の処理
        if (gameState.IsPaused)
        {
            _audioPlayback.PauseAll();
        }
        else
        {
            _audioPlayback.ResumeAll();
        }

        // 駅から離れた場合、メロディーを停止
        if (!gameState.IsAtStation)
        {
            lock (_stateLock)
            {
                if (_currentState.IsPlaying)
                {
                    _logger.LogInformation("駅から離れたため、メロディーを停止します");
                    _audioPlayback.StopMelody();
                    _currentState = _currentState.With(isPlaying: false);
                    StateChanged?.Invoke(this, _currentState);
                }
            }
        }
    }
}
