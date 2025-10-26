using System.ComponentModel;
using System.Windows;
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

    private GameState _currentGameState = new();

    public MainWindow(
        IMelodyControlService melodyControl,
        IAutoModeService autoMode,
        ITraincrewGameService gameService,
        ITrackRepository trackRepository,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();

        _melodyControl = melodyControl ?? throw new ArgumentNullException(nameof(melodyControl));
        _autoMode = autoMode ?? throw new ArgumentNullException(nameof(autoMode));
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _trackRepository = trackRepository ?? throw new ArgumentNullException(nameof(trackRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Loaded += OnLoaded;
        Closing += OnClosing;

        _melodyControl.StateChanged += OnMelodyStateChanged;
        _gameService.GameStateChanged += OnGameStateChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _gameService.ConnectAsync();
            _logger.LogInformation("アプリケーション起動完了");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "起動時エラー");
            MessageBox.Show("Traincrewゲームに接続できませんでした", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        await _gameService.DisconnectAsync();
        await _autoMode.StopAsync();
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

    private void OnMelodyStateChanged(object? sender, MelodyState state)
    {
        // UIスレッドで更新
        Dispatcher.Invoke(() =>
        {
            UpdateButtonStates();
        });
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
}