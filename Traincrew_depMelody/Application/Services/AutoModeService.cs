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

    private Timer? _checkTimer;
    private AutoModeConfig _config = new() { IsEnabled = false };
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
        ILogger<AutoModeService> logger)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _melodyControl = melodyControl ?? throw new ArgumentNullException(nameof(melodyControl));
        _audioPlayback = audioPlayback ?? throw new ArgumentNullException(nameof(audioPlayback));
        _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    public async Task StartAsync()
    {
        lock (_configLock)
        {
            _config = _config with { IsEnabled = true };
        }

        _logger.LogInformation("自動モード開始");

        // 16ミリ秒周期でチェック
        _checkTimer = new(async _ => await CheckAndExecuteAsync(), null, 0, 16);

        await Task.CompletedTask;
    }

    /// <summary>
    ///     自動モードを停止
    /// </summary>
    public async Task StopAsync()
    {
        lock (_configLock)
        {
            _config = _config with { IsEnabled = false };
        }

        _checkTimer?.Dispose();
        _checkTimer = null;

        _logger.LogInformation("自動モード停止");

        await Task.CompletedTask;
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
    ///     自動モードの条件チェックと実行
    /// </summary>
    private async Task CheckAndExecuteAsync()
    {
        if (!IsEnabled) return;

        try
        {
            var gameState = await _gameService.GetCurrentGameStateAsync();

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

        var shouldStart = false;

        // 条件1: 到着後1秒後
        if (_arrivalTime != null && (now - _arrivalTime.Value).TotalSeconds >= config.DelayAfterArrival)
        {
            shouldStart = true;
            _logger.LogDebug("条件1満たす: 到着後1秒");
        }

        // 条件2: 信号開通0.5秒後
        if (_signalOpenTime != null && (now - _signalOpenTime.Value).TotalSeconds >= config.DelayAfterSignalOpen)
        {
            shouldStart = true;
            _logger.LogDebug("条件2満たす: 信号開通0.5秒後");
        }

        // 条件3: 発車時刻ベース
        if (trainState.DepartureTime != null && gameState.CurrentCircuitId.Any())
        {
            var track = await _trackRepository.FindTrackByCircuitIdAsync(gameState.CurrentCircuitId.First());
            if (track != null)
            {
                var melodyDuration = await _audioPlayback.GetMelodyDurationAsync(track);
                var margin = config.GetMarginForVehicle(trainState);
                var totalOffset = melodyDuration + config.DoorCloseAnnouncementDuration + margin;

                var targetTime = trainState.DepartureTime.Value.Subtract(TimeSpan.FromSeconds(totalOffset));

                if (now >= targetTime)
                {
                    shouldStart = true;
                    _logger.LogDebug("条件3満たす: 発車時刻ベース (発車予定: {TrainStateDepartureTime})", trainState.DepartureTime);
                }
            }
        }

        if (shouldStart)
        {
            _logger.LogInformation("自動モード: メロディー開始");
            await _melodyControl.StartMelodyAsync();
            _melodyStartTime = gameState.CurrentGameTime; // ゲーム内時刻を使用
            _melodyTriggered = true;
        }
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

        // 条件3: 発車時刻ベース
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
    }
}