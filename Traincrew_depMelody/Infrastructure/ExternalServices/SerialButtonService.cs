using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Infrastructure.ExternalServices;

/// <summary>
///     シリアルポート経由の物理ボタン入力サービス。
///     System.Threading.Timer で InputPin をポーリングし、
///     2回連続同値でデバウンス確定後に ButtonStateChanged を発火する。
/// </summary>
public sealed class SerialButtonService : ISerialButtonService, IDisposable
{
    private readonly ILogger<SerialButtonService> _logger;
    private readonly object _lock = new();

    private SerialPort? _serialPort;
    private System.Threading.Timer? _timer;
    private SerialButtonConfig? _config;

    // デバウンス用: 前回確定値と候補値の連続カウント
    private bool _confirmedState;
    private bool _candidateState;
    private int _candidateCount;

    public bool IsConnected => _serialPort?.IsOpen == true;
    public bool CurrentState => _confirmedState;

    public event EventHandler<bool>? ButtonStateChanged;

    public SerialButtonService(ILogger<SerialButtonService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ConnectAsync(SerialButtonConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        // 既存接続があれば先に切断 (lock 外で呼ぶことでデッドロックを回避)
        DisconnectCore();

        lock (_lock)
        {
            _config = config;
            _serialPort = new SerialPort(config.PortName)
            {
                BaudRate = config.BaudRate,
                DtrEnable = config.DtrEnable,
                RtsEnable = config.RtsEnable
            };

            _serialPort.Open();
            _logger.LogInformation("シリアルポート {Port} に接続しました (Pin={Pin}, Interval={Interval}ms)",
                config.PortName, config.InputPin, config.PollingIntervalMs);

            // デバウンス状態をリセット
            _confirmedState = false;
            _candidateState = false;
            _candidateCount = 0;

            _timer = new System.Threading.Timer(
                OnTimerTick,
                null,
                config.PollingIntervalMs,
                config.PollingIntervalMs);
        }

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        // lock 外で呼ぶことでコールバック完了待ち中のデッドロックを回避
        DisconnectCore();

        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    /// <summary>
    ///     タイマーとシリアルポートを安全に解放する。
    ///     lock を持たずに呼び出すこと (WaitHandle 待ちを lock 外で行うためデッドロック回避)。
    /// </summary>
    private void DisconnectCore()
    {
        // Step 1: lock 内でタイマーを取り出し、フィールドを null にする
        System.Threading.Timer? timer;
        lock (_lock)
        {
            timer = _timer;
            _timer = null;
        }

        // Step 2: lock 外でコールバック完了を待ってから破棄
        if (timer != null)
        {
            using var waitHandle = new ManualResetEvent(false);
            timer.Dispose(waitHandle);
            waitHandle.WaitOne();
        }

        // Step 3: lock 内でシリアルポートを閉じて解放
        lock (_lock)
        {
            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        _logger.LogInformation("シリアルポート {Port} を切断しました", _serialPort.PortName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "シリアルポートのクローズ中にエラーが発生しました");
                }
                finally
                {
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
        }
    }

    private void OnTimerTick(object? state)
    {
        SerialPort? port;
        SerialButtonConfig? config;

        lock (_lock)
        {
            port = _serialPort;
            config = _config;
        }

        if (port == null || config == null || !port.IsOpen)
            return;

        bool rawValue;
        try
        {
            rawValue = config.InputPin switch
            {
                SerialInputPin.Dsr => port.DsrHolding,
                SerialInputPin.Cts => port.CtsHolding,
                SerialInputPin.Cd  => port.CDHolding,
                _ => port.DsrHolding
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "シリアルポート読み取りエラー");
            return;
        }

        // 論理反転
        bool logicalValue = config.Inverted ? !rawValue : rawValue;

        ProcessNewValue(logicalValue);
    }

    /// <summary>
    ///     簡易デバウンス: 2回連続で同じ値が来たら確定して発火。
    /// </summary>
    private void ProcessNewValue(bool newValue)
    {
        bool fireEvent = false;
        bool eventValue = false;

        lock (_lock)
        {
            if (newValue == _candidateState)
            {
                _candidateCount++;
                if (_candidateCount >= 2 && newValue != _confirmedState)
                {
                    _confirmedState = newValue;
                    _candidateCount = 0;
                    fireEvent = true;
                    eventValue = newValue;
                }
            }
            else
            {
                // 候補値が変わったのでカウントリセット
                _candidateState = newValue;
                _candidateCount = 1;
            }
        }

        if (fireEvent)
        {
            _logger.LogDebug("ボタン状態変化: {State}", eventValue ? "ON" : "OFF");
            ButtonStateChanged?.Invoke(this, eventValue);
        }
    }

    public void Dispose()
    {
        // lock 外で呼ぶことでコールバック完了待ち中のデッドロックを回避
        DisconnectCore();
    }
}
