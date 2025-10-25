using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;
using TrainCrew;
using CrewType = TrainCrew.CrewType;
using DomainGameState = Traincrew_depMelody.Domain.Models.GameState;
using DomainGameScreen = Traincrew_depMelody.Domain.Models.GameScreen;
using DomainCrewType = Traincrew_depMelody.Domain.Models.CrewType;
using DomainTrainState = Traincrew_depMelody.Domain.Models.TrainState;
using DomainSignalInfo = Traincrew_depMelody.Domain.Models.SignalInfo;

namespace Traincrew_depMelody.Infrastructure.ExternalServices;

[Serializable]
internal class CommandToTrainCrew
{
    public string command { get; init; }
    public string[] args { get; init; }
}

[Serializable]
internal class TraincrewBaseData
{
    public string type { get; init; }
    public object data { get; init; }
}

[Serializable]
internal class TrainCrewState
{
    public string type { get; init; }
    public TrainCrewStateData data { get; init; }
}

[Serializable]
internal class TrainCrewStateData
{
    public List<TrackCircuitData> trackCircuitList { get; init; } = [];
}

[Serializable]
internal class TrackCircuitData
{
    public bool On { get; set; } = false;
    public bool Lock { get; set; } = false;
    public string Last { get; set; } = null;
    public string Name { get; set; } = "";

    public override string ToString()
    {
        return $"{Name}";
    }
}

public class TraincrewGameService : ITraincrewGameService, IDisposable
{
    private const string DataRequestCommand = "DataRequest";
    private const string ConnectUri = "ws://127.0.0.1:50300/";
    private static readonly string[] DataRequestArgs = ["tconlyontrain"];
    private static readonly Encoding Encoding = Encoding.UTF8;

    private readonly AppConfiguration _config;
    private readonly ILogger<TraincrewGameService> _logger;

    private bool _isConnected;
    private string _trainNumber = string.Empty;
    private ClientWebSocket _webSocket = new();
    private List<string> _trackCircuits = [];
    private readonly SemaphoreSlim _fetchDataSemaphore = new(1, 1);

    public bool IsConnected => _isConnected;

    public event EventHandler<DomainGameState>? GameStateChanged;

    public TraincrewGameService(AppConfiguration config, ILogger<TraincrewGameService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// ゲームに接続
    /// </summary>
    public Task ConnectAsync()
    {
        TrainCrewInput.Init();
        _isConnected = true;
        _logger.LogInformation("Traincrewゲームに接続しました");
        return Task.CompletedTask;
    }

    /// <summary>
    /// ゲームから切断
    /// </summary>
    public Task DisconnectAsync()
    {
        TrainCrewInput.Dispose();
        _isConnected = false;
        _logger.LogInformation("Traincrewゲームから切断しました");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 現在のゲーム状態を取得
    /// </summary>
    public async Task<DomainGameState> GetCurrentGameStateAsync()
    {
        // データ取得を実行
        await FetchDataAsync();

        try
        {
            // TrainCrewInput.dllから情報を取得
            TrainCrewInput.RequestData(DataRequest.Signal);
            var trainState = TrainCrewInput.GetTrainState();
            var gameScreen = TrainCrewInput.gameState.gameScreen;

            // ゲーム内時刻を取得 
            var currentGameTime = trainState.NowTime;

            var crewType = DomainCrewType.None;
            if (gameScreen is TrainCrew.GameScreen.MainGame or TrainCrew.GameScreen.MainGame_Pause)
            {
                crewType = TrainCrewInput.gameState.crewType switch
                {
                    CrewType.Driver => DomainCrewType.Driver,
                    CrewType.Conductor => DomainCrewType.Conductor,
                    _ => DomainCrewType.None
                };
            }


            // TrainState構築
            var trainStateModel = new DomainTrainState
            {
                Speed = trainState.Speed,
                IsDoorsOpen = !trainState.AllClose,
                TrainNumber = trainState.diaName,
                VehicleTypes = trainState.CarStates.Select(c => c.CarModel).ToList(),
                DepartureTime = null, // TrainCrewには発車時刻の情報がない
            };

            var signalAspect = TrainCrewInput.signals.Any(s => s.phase != "R")
                ? SignalAspect.Proceed
                : SignalAspect.Stop;
            // SignalInfo構築
            var signalInfo = new DomainSignalInfo
            {
                Aspect = signalAspect,
                OpenedAt = null
            };

            // 軌道回路情報を取得
            string? currentCircuitId = _trackCircuits.FirstOrDefault();
            bool isAtStation = _trackCircuits.Any();

            return new DomainGameState
            {
                Screen = gameScreen switch
                {
                    TrainCrew.GameScreen.MainGame => DomainGameScreen.Driving,
                    TrainCrew.GameScreen.MainGame_Pause => DomainGameScreen.Driving,
                    _ => DomainGameScreen.Other
                },
                IsPaused = gameScreen == TrainCrew.GameScreen.MainGame_Pause,
                CrewType = crewType, 
                TrainState = trainStateModel,
                SignalInfo = signalInfo,
                CurrentCircuitId = currentCircuitId,
                IsAtStation = isAtStation,
                CurrentGameTime = currentGameTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ゲーム状態取得中にエラーが発生");
            return new DomainGameState
            {
                Screen = DomainGameScreen.Other,
                IsPaused = false,
                CrewType = DomainCrewType.None,
                CurrentGameTime = TimeSpan.Zero,
                IsAtStation = false
            };
        }
    }

    /// <summary>
    /// データ取得処理
    /// </summary>
    private async Task FetchDataAsync()
    {
        // 既に実行中の場合は待たずに即座にreturn
        if (!await _fetchDataSemaphore.WaitAsync(0))
        {
            return;
        }

        try
        {
            TrainCrewInput.RequestData(DataRequest.Signal);
            var trainState = TrainCrewInput.GetTrainState();
            _trainNumber = trainState.diaName;

            if (TrainCrewInput.gameState.gameScreen
                is not (TrainCrew.GameScreen.MainGame or TrainCrew.GameScreen.MainGame_Pause))
            {
                return;
            }

            while (_webSocket.State != WebSocketState.Open)
            {
                try
                {
                    await _webSocket.ConnectAsync(new(ConnectUri), CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    _webSocket.Dispose();
                    _webSocket = new();
                    return;
                }
                catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
                {
                    _webSocket.Dispose();
                    _webSocket = new();
                }
            }

            if (IsGameRunning() && _webSocket.State == WebSocketState.Open)
            {
                await SendMessagesAsync();
                await ReceiveMessagesAsync(_trainNumber);
            }
        }
        finally
        {
            _fetchDataSemaphore.Release();
        }
    }

    /// <summary>
    /// WebSocketでメッセージを送信
    /// </summary>
    private async Task SendMessagesAsync()
    {
        CommandToTrainCrew requestCommand = new()
        {
            command = DataRequestCommand,
            args = DataRequestArgs
        };

        var json = JsonSerializer.Serialize(requestCommand);
        var bytes = Encoding.GetBytes(json);

        try
        {
            await _webSocket.SendAsync(new(bytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        catch (WebSocketException)
        {
            _webSocket.Dispose();
        }
    }

    /// <summary>
    /// WebSocketでメッセージを受信
    /// </summary>
    private async Task ReceiveMessagesAsync(string trainNumber)
    {
        var buffer = new byte[2048];

        if (_webSocket.State != WebSocketState.Open)
        {
            return;
        }

        List<byte> messageBytes = [];
        WebSocketReceiveResult result;
        do
        {
            result = await _webSocket.ReceiveAsync(new(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            messageBytes.AddRange(buffer.Take(result.Count));
        } while (!result.EndOfMessage);

        var jsonResponse = Encoding.GetString(messageBytes.ToArray());
        messageBytes.Clear();

        var traincrewBaseData = JsonSerializer.Deserialize<TraincrewBaseData>(jsonResponse);

        if (traincrewBaseData == null)
        {
            return;
        }

        if (traincrewBaseData.type != "TrainCrewStateData")
        {
            return;
        }

        var dataJsonElement = (JsonElement)traincrewBaseData.data;
        var trainCrewStateData = JsonSerializer.Deserialize<TrainCrewStateData>(dataJsonElement.GetRawText());

        if (trainCrewStateData == null)
        {
            return;
        }

        _trackCircuits = trainCrewStateData
            .trackCircuitList
            .Where(trackCircuit => trackCircuit.Last == trainNumber)
            .Select(trackCircuit => trackCircuit.Name)
            .ToList();
    }

    /// <summary>
    /// ゲームステータスを取得
    /// </summary>
    private bool IsGameRunning()
    {
        return TrainCrewInput.gameState.gameScreen switch
        {
            TrainCrew.GameScreen.MainGame => true,
            _ => false
        };
    }

    public void Dispose()
    {
        TrainCrewInput.Dispose();
        _webSocket.Dispose();
        _fetchDataSemaphore.Dispose();
    }
}