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

    private GameState _currentGameState = new();
    private DispatcherTimer? _updateTimer;

    public MainWindow(
        IMelodyControlService melodyControl,
        IAutoModeService autoMode,
        ITraincrewGameService gameService,
        ITrackRepository trackRepository,
        ISerialButtonService serialButton,
        SerialButtonConfig serialButtonConfig,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();

        _melodyControl = melodyControl ?? throw new ArgumentNullException(nameof(melodyControl));
        _autoMode = autoMode ?? throw new ArgumentNullException(nameof(autoMode));
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
        _serialButton = serialButton ?? throw new ArgumentNullException(nameof(serialButton));
        _serialButtonConfig = serialButtonConfig ?? throw new ArgumentNullException(nameof(serialButtonConfig));
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

        // 入力線ComboBoxを初期化
        InitializeSerialUi();

        // appsettings でポートが指定されていれば自動接続を試みる
        if (!string.IsNullOrEmpty(_serialButtonConfig.PortName))
        {
            try
            {
                await _serialButton.ConnectAsync(_serialButtonConfig);
                UpdateSerialStatus();
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

    // ─── シリアルポート UI ────────────────────────────────────────────

    /// <summary>
    ///     シリアル設定UIの初期値を設定する
    /// </summary>
    private void InitializeSerialUi()
    {
        // 入力線選択肢を設定
        ComboBoxPin.Items.Clear();
        foreach (var pin in Enum.GetNames<SerialInputPin>())
        {
            ComboBoxPin.Items.Add(pin);
        }
        ComboBoxPin.SelectedItem = _serialButtonConfig.InputPin.ToString();

        // 反転チェックボックス
        CheckBoxInverted.IsChecked = _serialButtonConfig.Inverted;

        // ポート一覧を読み込む
        RefreshPortList();

        // appsettings のポートが一覧にあれば選択
        if (!string.IsNullOrEmpty(_serialButtonConfig.PortName)
            && ComboBoxPort.Items.Contains(_serialButtonConfig.PortName))
        {
            ComboBoxPort.SelectedItem = _serialButtonConfig.PortName;
        }
    }

    /// <summary>
    ///     利用可能なポート一覧をComboBoxに読み込む
    /// </summary>
    private void RefreshPortList()
    {
        var current = ComboBoxPort.SelectedItem?.ToString();
        ComboBoxPort.Items.Clear();
        foreach (var port in _serialButton.GetAvailablePorts())
        {
            ComboBoxPort.Items.Add(port);
        }

        // 以前選択していたポートが残っていれば復元
        if (current != null && ComboBoxPort.Items.Contains(current))
        {
            ComboBoxPort.SelectedItem = current;
        }
        else if (ComboBoxPort.Items.Count > 0)
        {
            ComboBoxPort.SelectedIndex = 0;
        }
    }

    /// <summary>
    ///     接続状態に応じてUIの状態ラベルとボタン活性を更新する
    /// </summary>
    private void UpdateSerialStatus()
    {
        if (_serialButton.IsConnected)
        {
            var port = ComboBoxPort.SelectedItem?.ToString() ?? "";
            var stateText = _serialButton.CurrentState ? "ON" : "OFF";
            LabelSerialStatus.Content = $"接続中: {port} ({stateText})";
            ButtonConnect.IsEnabled = false;
            ButtonDisconnect.IsEnabled = true;
        }
        else
        {
            LabelSerialStatus.Content = "未接続";
            ButtonConnect.IsEnabled = true;
            ButtonDisconnect.IsEnabled = false;
        }
    }

    private void ButtonRefreshPorts_Click(object sender, RoutedEventArgs e)
    {
        RefreshPortList();
    }

    private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
    {
        var portName = ComboBoxPort.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(portName))
        {
            MessageBox.Show("ポートを選択してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pinText = ComboBoxPin.SelectedItem?.ToString();
        if (!Enum.TryParse<SerialInputPin>(pinText, out var pin))
        {
            pin = SerialInputPin.Dsr;
        }

        var config = new SerialButtonConfig
        {
            PortName = portName,
            BaudRate = _serialButtonConfig.BaudRate,
            DtrEnable = _serialButtonConfig.DtrEnable,
            RtsEnable = _serialButtonConfig.RtsEnable,
            InputPin = pin,
            PollingIntervalMs = _serialButtonConfig.PollingIntervalMs,
            Inverted = CheckBoxInverted.IsChecked == true
        };

        try
        {
            await _serialButton.ConnectAsync(config);
            UpdateSerialStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "シリアルポート接続エラー");
            MessageBox.Show($"接続エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _serialButton.DisconnectAsync();
            UpdateSerialStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "シリアルポート切断エラー");
            MessageBox.Show($"切断エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // 状態ラベルをUIスレッドで更新
            Dispatcher.Invoke(UpdateSerialStatus);
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
