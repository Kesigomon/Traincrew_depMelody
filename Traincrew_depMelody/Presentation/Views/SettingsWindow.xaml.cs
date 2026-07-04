using System.Windows;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Presentation.Views;

public partial class SettingsWindow : Window
{
    private readonly ISerialButtonService _serialButton;
    private readonly SerialButtonConfig _serialButtonConfig;
    private readonly ILogger<SettingsWindow> _logger;
    private readonly IAudioProfileRepository _profileRepository;
    private readonly IAudioPlaybackService _audioPlaybackService;
    private readonly IConfigurationPersistenceService _persistence;
    private readonly AppConfiguration _appConfiguration;

    public event EventHandler<TopmostMode>? TopmostModeChanged;

    public SettingsWindow(
        ISerialButtonService serialButton,
        SerialButtonConfig serialButtonConfig,
        ILogger<SettingsWindow> logger,
        IAudioProfileRepository profileRepository,
        IAudioPlaybackService audioPlaybackService,
        IConfigurationPersistenceService persistence,
        AppConfiguration appConfiguration)
    {
        InitializeComponent();

        _serialButton = serialButton ?? throw new ArgumentNullException(nameof(serialButton));
        _serialButtonConfig = serialButtonConfig ?? throw new ArgumentNullException(nameof(serialButtonConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _audioPlaybackService = audioPlaybackService ?? throw new ArgumentNullException(nameof(audioPlaybackService));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

        Loaded += OnLoaded;
        Closed += OnClosed;

        // 状態ラベル更新のためにイベントを購読する
        _serialButton.ButtonStateChanged += OnSerialButtonStateChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeSerialUi();
        InitializeProfileUi();
        InitializeTopmostUi();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _serialButton.ButtonStateChanged -= OnSerialButtonStateChanged;
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

        // 現在の接続状態を反映する
        UpdateSerialStatus();
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
    private void OnSerialButtonStateChanged(object? sender, bool isOn)
    {
        // 状態ラベルをUIスレッドで更新する
        Dispatcher.Invoke(UpdateSerialStatus);
    }

    // ─── プロファイル UI ──────────────────────────────────────────────

    /// <summary>
    ///     プロファイル設定UIの初期値を設定する
    /// </summary>
    private void InitializeProfileUi()
    {
        ComboBoxProfile.Items.Clear();
        foreach (var name in _profileRepository.GetAvailableProfileNames())
        {
            ComboBoxProfile.Items.Add(name);
        }
        ComboBoxProfile.SelectedItem = _profileRepository.CurrentProfileName;
        LabelProfileStatus.Content = $"現在: {_profileRepository.CurrentProfileName}";
    }

    private async void ButtonApplyProfile_Click(object sender, RoutedEventArgs e)
    {
        var selected = ComboBoxProfile.SelectedItem as string;
        if (string.IsNullOrEmpty(selected))
        {
            MessageBox.Show("プロファイルを選択してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selected == _profileRepository.CurrentProfileName) return;

        try
        {
            _audioPlaybackService.StopAll();
            await _profileRepository.SwitchProfileAsync(selected);
            await _persistence.SaveProfileNameAsync(selected);
            LabelProfileStatus.Content = $"現在: {selected}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "プロファイル切り替えエラー");
            MessageBox.Show($"プロファイル切り替えエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─── 最前面表示 UI ────────────────────────────────────────────────

    private void InitializeTopmostUi()
    {
        switch (_appConfiguration.TopmostMode)
        {
            case TopmostMode.Never:
                RadioTopmostNever.IsChecked = true;
                break;
            case TopmostMode.WhenButtonEnabled:
                RadioTopmostWhenButtonEnabled.IsChecked = true;
                break;
            case TopmostMode.WhilePlaying:
                RadioTopmostWhilePlaying.IsChecked = true;
                break;
            case TopmostMode.Always:
                RadioTopmostAlways.IsChecked = true;
                break;
        }
        UpdateTopmostStatusLabel();
    }

    private void UpdateTopmostStatusLabel()
    {
        var text = _appConfiguration.TopmostMode switch
        {
            TopmostMode.Never => "しない",
            TopmostMode.WhenButtonEnabled => "ボタンが押せる場合にする",
            TopmostMode.WhilePlaying => "プレイ中(ポーズ中含め)にする",
            TopmostMode.Always => "常にする",
            _ => ""
        };
        LabelTopmostStatus.Content = $"現在: {text}";
    }

    private TopmostMode GetSelectedTopmostMode()
    {
        if (RadioTopmostWhenButtonEnabled.IsChecked == true) return TopmostMode.WhenButtonEnabled;
        if (RadioTopmostWhilePlaying.IsChecked == true) return TopmostMode.WhilePlaying;
        if (RadioTopmostAlways.IsChecked == true) return TopmostMode.Always;
        return TopmostMode.Never;
    }

    private async void ButtonApplyTopmost_Click(object sender, RoutedEventArgs e)
    {
        var mode = GetSelectedTopmostMode();
        if (mode == _appConfiguration.TopmostMode) return;

        try
        {
            _appConfiguration.TopmostMode = mode;
            await _persistence.SaveTopmostModeAsync(mode);
            UpdateTopmostStatusLabel();
            TopmostModeChanged?.Invoke(this, mode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "最前面表示設定の保存エラー");
            MessageBox.Show($"設定保存エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
