using System.Windows;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Presentation.Views;

public partial class SettingsWindow : Window
{
    private readonly ISerialButtonService _serialButton;
    private readonly SerialButtonConfig _serialButtonConfig;
    private readonly ILogger<SettingsWindow> _logger;

    public SettingsWindow(
        ISerialButtonService serialButton,
        SerialButtonConfig serialButtonConfig,
        ILogger<SettingsWindow> logger)
    {
        InitializeComponent();

        _serialButton = serialButton ?? throw new ArgumentNullException(nameof(serialButton));
        _serialButtonConfig = serialButtonConfig ?? throw new ArgumentNullException(nameof(serialButtonConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Loaded += OnLoaded;
        Closed += OnClosed;

        // 状態ラベル更新のためにイベントを購読する
        _serialButton.ButtonStateChanged += OnSerialButtonStateChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeSerialUi();
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
}
