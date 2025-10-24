using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Infrastructure.ExternalServices;

public class TraincrewGameService : ITraincrewGameService, IDisposable
{
    private readonly AppConfiguration _config;
    private readonly ILogger<TraincrewGameService> _logger;

    private ClientWebSocket? _webSocket;
    private bool _isConnected;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _isConnected;

    public event EventHandler<GameState>? GameStateChanged;

    // TrainCrewInput.dll のメソッド定義(P/Invoke)
    [DllImport("TrainCrewInput.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetGameScreen();

    [DllImport("TrainCrewInput.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool IsPaused();

    [DllImport("TrainCrewInput.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetCrewType();

    [DllImport("TrainCrewInput.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern double GetTrainSpeed();

    [DllImport("TrainCrewInput.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool IsDoorsOpen();

    [DllImport("TrainCrewInput.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetTrainNumber();

    [DllImport("TrainCrewInput.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetVehicleType();

    [DllImport("TrainCrewInput.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetDepartureTime();

    [DllImport("TrainCrewInput.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetSignalAspect();

    public TraincrewGameService(AppConfiguration config, ILogger<TraincrewGameService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// ゲームに接続
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_isConnected)
        {
            _logger.LogWarning("既に接続済みです");
            return;
        }

        try
        {
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            string uri = $"ws://localhost:{_config.WebSocketPort}";
            await _webSocket.ConnectAsync(new Uri(uri), _cts.Token);

            _isConnected = true;
            _logger.LogInformation("Traincrewゲームに接続しました");

            // 受信ループを開始
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Traincrewゲームへの接続に失敗");
            throw;
        }
    }

    /// <summary>
    /// ゲームから切断
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (!_isConnected) return;

        _cts?.Cancel();

        if (_webSocket != null)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            _webSocket.Dispose();
            _webSocket = null;
        }

        _isConnected = false;
        _logger.LogInformation("Traincrewゲームから切断しました");
    }

    /// <summary>
    /// 現在のゲーム状態を取得
    /// </summary>
    public async Task<GameState> GetCurrentGameStateAsync()
    {
        try
        {
            // TrainCrewInput.dllから情報を取得
            var screen = (GameScreen)GetGameScreen();
            bool isPaused = IsPaused();
            var crewType = (CrewType)GetCrewType();

            // TrainState構築
            double speed = GetTrainSpeed();
            bool isDoorsOpen = IsDoorsOpen();
            string? trainNumber = Marshal.PtrToStringAnsi(GetTrainNumber());
            string? vehicleType = Marshal.PtrToStringAnsi(GetVehicleType());
            int departureTimeUnix = GetDepartureTime();
            DateTime? departureTime = departureTimeUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(departureTimeUnix).DateTime : null;

            var trainState = new TrainState
            {
                Speed = speed,
                IsDoorsOpen = isDoorsOpen,
                TrainNumber = trainNumber,
                VehicleType = vehicleType,
                DepartureTime = departureTime,
                ArrivalTime = null // TODO: 実装
            };

            // SignalInfo構築
            int signalAspectValue = GetSignalAspect();
            var signalAspect = signalAspectValue == 0 ? SignalAspect.Stop : SignalAspect.Proceed;
            var signalInfo = new SignalInfo
            {
                Aspect = signalAspect,
                OpenedAt = null // TODO: 状態遷移検知が必要
            };

            // WebSocketから軌道回路情報を取得(簡略化)
            string? currentCircuitId = await GetCurrentCircuitIdAsync();

            bool isAtStation = !string.IsNullOrEmpty(currentCircuitId); // 簡易判定

            return new GameState
            {
                Screen = screen,
                IsPaused = isPaused,
                CrewType = crewType,
                TrainState = trainState,
                SignalInfo = signalInfo,
                CurrentCircuitId = currentCircuitId,
                IsAtStation = isAtStation,
                CurrentGameTime = DateTime.Now // TODO: ゲーム時刻の取得
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ゲーム状態取得中にエラーが発生");
            // デフォルト値を返す
            return new GameState
            {
                Screen = GameScreen.Other,
                IsPaused = false,
                CrewType = CrewType.None,
                CurrentGameTime = DateTime.Now,
                IsAtStation = false
            };
        }
    }

    /// <summary>
    /// WebSocketから軌道回路IDを取得
    /// </summary>
    private async Task<string?> GetCurrentCircuitIdAsync()
    {
        // TODO: 実際のWebSocket通信実装
        // 軌道回路情報を取得する処理を実装
        await Task.CompletedTask;
        return null; // プレースホルダー
    }

    /// <summary>
    /// WebSocket受信ループ
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket != null)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                // メッセージを処理
                string message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                ProcessWebSocketMessage(message);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常なキャンセル
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket受信中にエラーが発生");
        }
    }

    /// <summary>
    /// WebSocketメッセージを処理
    /// </summary>
    private void ProcessWebSocketMessage(string message)
    {
        // TODO: JSONパース等の実装
        _logger.LogTrace($"WebSocketメッセージ受信: {message}");
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
        _cts?.Dispose();
    }
}
