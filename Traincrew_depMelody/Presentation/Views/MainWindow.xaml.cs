using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Presentation.Views;

public partial class MainWindow : Window
{
    private readonly IAutoModeService _autoMode;
    private readonly ITraincrewGameService _gameService;
    private readonly ILogger<MainWindow> _logger;
    private readonly IMelodyControlService _melodyControl;
    private readonly ITrackRepository _trackRepository;
    private readonly ISerialButtonService _serialButton;
    private readonly SerialButtonConfig _serialButtonConfig;
    private readonly Func<SettingsWindow> _settingsWindowFactory;

    private GameState _currentGameState = new();
    private DispatcherTimer? _updateTimer;
    private SettingsWindow? _settingsWindow;

    public MainWindow(
        IMelodyControlService melodyControl,
        IAutoModeService autoMode,
        ITraincrewGameService gameService,
        ITrackRepository trackRepository,
        ISerialButtonService serialButton,
        SerialButtonConfig serialButtonConfig,
        Func<SettingsWindow> settingsWindowFactory,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();

        _melodyControl = melodyControl ?? throw new ArgumentNullException(nameof(melodyControl));
        _autoMode = autoMode ?? throw new ArgumentNullException(nameof(autoMode));
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
        _serialButton = serialButton ?? throw new ArgumentNullException(nameof(serialButton));
        _serialButtonConfig = serialButtonConfig ?? throw new ArgumentNullException(nameof(serialButtonConfig));
        _settingsWindowFactory = settingsWindowFactory ?? throw new ArgumentNullException(nameof(settingsWindowFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Loaded += OnLoaded;
        Closing += OnClosing;

        _gameService.GameStateChanged += OnGameStateChanged;
        _serialButton.ButtonStateChanged += OnSerialButtonStateChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _gameService.ConnectAsync();

            // 16ms周期でゲーム状態を更新するタイマーを開始
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _updateTimer.Tick += async (s, args) => await UpdateGameStateAsync();
            _updateTimer.Start();

            _logger.LogInformation("アプリケーション起動完了");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "起動時エラー");
            MessageBox.Show("Traincrewゲームに接続できませんでした", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // appsettings でポートが指定されていれば自動接続を試みる
        if (!string.IsNullOrEmpty(_serialButtonConfig.PortName))
        {
            try
            {
                await _serialButton.ConnectAsync(_serialButtonConfig);
                _logger.LogInformation("シリアルポート {Port} に自動接続しました", _serialButtonConfig.PortName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "シリアルポートの自動接続に失敗しました。手動で接続してください");
            }
        }
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        // 設定ウィンドウが開いていれば閉じる
        if (_settingsWindow != null)
        {
            _settingsWindow.Close();
            _settingsWindow = null;
        }

        _updateTimer?.Stop();
        _updateTimer = null;

        await _serialButton.DisconnectAsync();
        await _gameService.DisconnectAsync();
        await _autoMode.StopAsync();
    }

    private async Task UpdateGameStateAsync()
    {
        try
        {
            await _gameService.UpdateGameStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ゲーム状態更新エラー");
        }
    }

    private async void OnButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _melodyControl.StartMelodyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "メロディー開始エラー");
            MessageBox.Show($"エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OffButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _melodyControl.StopMelodyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "メロディー停止エラー");
            MessageBox.Show($"エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnGameStateChanged(object? sender, GameState state)
    {
        _currentGameState = state;
        // UIスレッドで更新
        Dispatcher.Invoke(() =>
        {
            UpdateButtonStates();
        });
    }

    private async void UpdateButtonStates()
    {
        // ボタン有効化条件:
        // 1. プレイ中 (GameScreen.Playing)
        // 2. 駅に在線している
        // 3. 自動モードオフ
        var isAtStation = await _trackRepository.IsAnyCircuitAtStationAsync(_currentGameState.CurrentCircuitId);
        var shouldEnable = _currentGameState.Screen == GameScreen.Playing
                           && isAtStation
                           && !_autoMode.IsEnabled;

        OnButton.IsEnabled = shouldEnable;
        OffButton.IsEnabled = shouldEnable;
    }

    /// <summary>
    ///     右クリックで設定ウィンドウを開く
    /// </summary>
    private void Window_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowSettingsWindow();
    }

    /// <summary>
    ///     設定ウィンドウのシングルインスタンス管理: 既存が表示中なら前面に出し、なければ新規生成して表示する
    /// </summary>
    private void ShowSettingsWindow()
    {
        if (_settingsWindow == null || !_settingsWindow.IsVisible)
        {
            _settingsWindow = _settingsWindowFactory();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    /// <summary>
    ///     シリアルボタン状態変化イベントハンドラ (ThreadPool スレッドから呼ばれる)
    /// </summary>
    private async void OnSerialButtonStateChanged(object? sender, bool isOn)
    {
        try
        {
            if (isOn)
            {
                await _melodyControl.StartMelodyAsync();
            }
            else
            {
                await _melodyControl.StopMelodyAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "シリアルボタン操作エラー");
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }
}
