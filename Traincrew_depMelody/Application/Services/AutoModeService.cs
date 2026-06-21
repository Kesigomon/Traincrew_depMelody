using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Application.Services;

public class AutoModeService : IAutoModeService
{
    private readonly IAudioPlaybackService _audioPlayback;
    private readonly object _configLock = new();
    private readonly ITraincrewGameService _gameService;
    private readonly ILogger<AutoModeService> _logger;
    private readonly IMelodyControlService _melodyControl;
    private readonly ITrackRepository _trackRepository;

    // 自動モードの状態追跡(ゲーム内時刻で記録)
    private TimeSpan? _arrivalTime;

    // メロディー長のffmpeg取得結果キャッシュ(ゲーム状態ポーリング16ms毎にffmpeg起動が走るのを防ぐため)
    private (string StationName, string TrackNumber, bool IsInbound, double Duration)? _cachedMelodyDuration;
    private AutoModeConfig _config;
    private TimeSpan? _doorOpenTime;
    private TimeSpan? _melodyStartTime;
    private bool _melodyTriggered;
    private bool _previousDoorsOpen; // ドア状態の変化を検知するための前回値
    private TimeSpan? _signalOpenTime;

    public AutoModeService(
        ITraincrewGameService gameService,
        IMelodyControlService melodyControl,
        IAudioPlaybackService audioPlayback,
        ITrackRepository trackRepository,
        AutoModeConfig initialConfig,
        ILogger<AutoModeService> logger)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _melodyControl = melodyControl ?? throw new ArgumentNullException(nameof(melodyControl));
        _audioPlayback = audioPlayback ?? throw new ArgumentNullException(nameof(audioPlayback));
        _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
        _config = initialConfig ?? throw new ArgumentNullException(nameof(initialConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ゲーム状態変化時のイベントハンドリング
        _gameService.GameStateChanged += OnGameStateChanged;
    }

    public bool IsEnabled
    {
        get
        {
            lock (_configLock)
            {
                return _config.IsEnabled;
            }
        }
    }

    /// <summary>
    ///     自動モードを開始
    /// </summary>
    public Task StartAsync()
    {
        lock (_configLock)
        {
            _config = _config with { IsEnabled = true };
        }

        _logger.LogInformation("自動モード開始");

        return Task.CompletedTask;
    }

    /// <summary>
    ///     自動モードを停止
    /// </summary>
    public Task StopAsync()
    {
        lock (_configLock)
        {
            _config = _config with { IsEnabled = false };
        }

        _logger.LogInformation("自動モード停止");

        return Task.CompletedTask;
    }

    /// <summary>
    ///     自動モード設定を取得
    /// </summary>
    public AutoModeConfig GetConfig()
    {
        lock (_configLock)
        {
            return _config;
        }
    }

    /// <summary>
    ///     自動モード設定を更新
    /// </summary>
    public void UpdateConfig(AutoModeConfig config)
    {
        lock (_configLock)
        {
            _config = config;
        }
    }

    /// <summary>
    ///     ゲーム状態変化時のイベントハンドラ
    /// </summary>
    private async void OnGameStateChanged(object? sender, GameState gameState)
    {
        if (!IsEnabled) return;

        try
        {
            // 運転士モード時のみ自動モード有効
            if (gameState.CrewType != CrewType.Driver) return;

            // プレイ中以外は何もしない
            if (gameState.Screen != GameScreen.Playing) return;

            // 駅に在線していない場合はリセット
            var isAtStation = await _trackRepository.IsAnyCircuitAtStationAsync(gameState.CurrentCircuitId);
            if (!isAtStation)
            {
                ResetState();
                return;
            }

            var trainState = gameState.TrainState;
            if (trainState == null) return;

            // 到着時刻を記録(ドアが閉→開に変化したゲーム内時刻)
            if (!_previousDoorsOpen && trainState.IsDoorsOpen && _arrivalTime == null)
            {
                _arrivalTime = gameState.CurrentGameTime;
                _logger.LogDebug("到着を検知(ドア開)");
            }

            // 信号開通時刻を記録(ゲーム内時刻)
            if (gameState.SignalInfo?.IsOpen == true && _signalOpenTime == null)
            {
                _signalOpenTime = gameState.CurrentGameTime;
                _logger.LogDebug("信号開通を検知");
            }

            // ドア開時刻を記録(ゲーム内時刻)
            if (trainState.IsDoorsOpen && _doorOpenTime == null)
            {
                _doorOpenTime = gameState.CurrentGameTime;
                _logger.LogDebug("ドア開を検知");
            }

            // 次回の比較のため現在のドア状態を保存
            _previousDoorsOpen = trainState.IsDoorsOpen;

            // メロディーON条件チェック
            await CheckMelodyOnConditionsAsync(gameState, trainState);

            // メロディーOFF条件チェック
            await CheckMelodyOffConditionsAsync(gameState, trainState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自動モードチェック中にエラーが発生");
        }
    }

    /// <summary>
    ///     メロディーON条件をチェック
    /// </summary>
    private async Task CheckMelodyOnConditionsAsync(GameState gameState, TrainState trainState)
    {
        if (_melodyTriggered) return;

        var config = GetConfig();
        var now = gameState.CurrentGameTime; // ゲーム内時刻を使用

        // 条件1: 到着後1秒後
        if (_arrivalTime == null || (now - _arrivalTime.Value).TotalSeconds < config.DelayAfterArrival)
        {
            return;
        }

        // 条件2: 信号開通0.5秒後
        if (_signalOpenTime == null || (now - _signalOpenTime.Value).TotalSeconds < config.DelayAfterSignalOpen)
        {
            return;
        }

        // 条件3: 発車時刻ベース(発車予定が分かっている場合のみブロック判定。分からない場合は条件1・2のみで判定)
        if (trainState.DepartureTime != null && gameState.CurrentCircuitId.Any())
        {
            var track = await _trackRepository.FindTrackByCircuitIdAsync(gameState.CurrentCircuitId, gameState.TrainClass);
            if (track != null)
            {
                var isInbound = trainState.IsInbound();
                double melodyDuration;
                // 駅名・番線・方向が前回と同一ならキャッシュを使う(同一区間に滞在中は毎ポーリングでffmpegを呼ばないため)
                if (_cachedMelodyDuration is { } cached &&
                    cached.StationName == track.StationName &&
                    cached.TrackNumber == track.TrackNumber &&
                    cached.IsInbound == isInbound)
                {
                    melodyDuration = cached.Duration;
                }
                else
                {
                    // キャッシュ未取得/別駅・別番線に変わった場合のみffmpegで取得し、結果を保持する
                    melodyDuration = await _audioPlayback.GetMelodyDurationAsync(track, isInbound);
                    _cachedMelodyDuration = (track.StationName, track.TrackNumber, isInbound, melodyDuration);
                }

                var margin = config.GetMarginForVehicle(trainState);
                var totalOffset = melodyDuration + config.DoorCloseAnnouncementDuration + margin;

                var targetTime = trainState.DepartureTime.Value.Subtract(TimeSpan.FromSeconds(totalOffset));

                if (now < targetTime)
                {
                    return;
                }

                _logger.LogDebug("条件3満たす: 発車時刻ベース (発車予定: {TrainStateDepartureTime})", trainState.DepartureTime);
            }
        }

        _logger.LogInformation("自動モード: メロディー開始");
        await _melodyControl.StartMelodyAsync();
        _melodyStartTime = gameState.CurrentGameTime; // ゲーム内時刻を使用
        _melodyTriggered = true;
    }

    /// <summary>
    ///     メロディーOFF条件をチェック
    /// </summary>
    private async Task CheckMelodyOffConditionsAsync(GameState gameState, TrainState trainState)
    {
        if (!_melodyTriggered) return;

        var melodyState = _melodyControl.GetCurrentState();
        if (!melodyState.IsPlaying) return;

        var config = GetConfig();
        var now = gameState.CurrentGameTime; // ゲーム内時刻を使用

        var shouldStop = false;

        // 条件1: ON後最低1秒後
        if (_melodyStartTime != null &&
            (now - _melodyStartTime.Value).TotalSeconds < config.MinimumMelodyDuration) return;

        // 条件2: ドア開後最低12秒後
        if (_doorOpenTime != null && (now - _doorOpenTime.Value).TotalSeconds < config.MinimumDoorOpenDuration) return;

        // 条件3: 発車時刻ベース（DepartureTimeがない場合は条件1+2を満たしたのでそのまま停止）
        if (trainState.DepartureTime != null)
        {
            var margin = config.GetMarginForVehicle(trainState);
            var totalOffset = config.DoorCloseAnnouncementDuration + margin;

            var targetTime = trainState.DepartureTime.Value.Subtract(TimeSpan.FromSeconds(totalOffset));

            if (now >= targetTime)
            {
                shouldStop = true;
                _logger.LogDebug("条件3満たす: 発車時刻ベース停止 (発車予定: {TrainStateDepartureTime})", trainState.DepartureTime);
            }
        }
        else
        {
            shouldStop = true;
            _logger.LogDebug("DepartureTimeがnull: 条件1+2で停止");
        }

        if (shouldStop)
        {
            _logger.LogInformation("自動モード: メロディー停止");
            await _melodyControl.StopMelodyAsync();
        }
    }

    /// <summary>
    ///     状態をリセット
    /// </summary>
    private void ResetState()
    {
        _arrivalTime = null;
        _signalOpenTime = null;
        _melodyStartTime = null;
        _doorOpenTime = null;
        _melodyTriggered = false;
        _previousDoorsOpen = false;
        // 駅離脱/状態リセット時にクリアし、次に別駅・別番線へ遷移した際は再取得させる
        _cachedMelodyDuration = null;
    }
}