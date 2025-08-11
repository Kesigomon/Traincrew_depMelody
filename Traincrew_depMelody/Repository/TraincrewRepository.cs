using System.Net.WebSockets;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using TrainCrew;

namespace Traincrew_depMelody.Repository;

public interface ITraincrewRepository
{
    Task Fetch();
    TrainState GetTrainState();
    HashSet<string> GetTrackCircuitSet();
    CrewType GetCrewType();
    GameScreen GetGameScreen();
    List<SignalInfo> GetSignalInfos();
}

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
    public string Last { get; set; } = null; // 軌道回路を踏んだ列車の名前
    public string Name { get; set; } = "";

    public override string ToString()
    {
        return $"{Name}";
    }
}

public class TraincrewRepository : ITraincrewRepository, IDisposable
{
    private const string DataRequestCommand = "DataRequest";
    private const string _connectUri = "ws://127.0.0.1:50300/"; //TRAIN CREWのポート番号は50300
    private static readonly string[] DataRequestArgs = ["tconlyontrain"];
    private static readonly Encoding _encoding = Encoding.UTF8;

    private TrainState? _trainState;
    private ClientWebSocket _webSocket = new();
    private HashSet<string> _trackCircuitSet = [];

    public TraincrewRepository()
    {
        TrainCrewInput.Init();
    }

    public void Dispose()
    {
        TrainCrewInput.Dispose();
        _webSocket.Dispose();
    }

    public async Task Fetch()
    {
        TrainCrewInput.RequestData(DataRequest.Signal);
        _trainState = TrainCrewInput.GetTrainState();
        while (_webSocket.State != WebSocketState.Open)
        {
            try
            {
                await _webSocket.ConnectAsync(new(_connectUri), CancellationToken.None);
            }
            catch (WebSocketException e)
            {
                _webSocket.Dispose();
                _webSocket = new();
                return;
            }
            catch (ObjectDisposedException)
            {
                _webSocket.Dispose();
                _webSocket = new();
            }
        }

        if (GetGameScreen() == GameScreen.MainGame && _webSocket.State == WebSocketState.Open)
        {
            await SendMessages();
            await ReceiveMessages(_trainState.diaName);
        }
    }

    private async Task SendMessages()
    {
        CommandToTrainCrew requestCommand = new()
        {
            command = DataRequestCommand,
            args = DataRequestArgs
        };

        // JSON形式にシリアライズ
        var json = JsonSerializer.Serialize(requestCommand);
        var bytes = _encoding.GetBytes(json);

        // WebSocket送信
        try
        {
            await _webSocket.SendAsync(new(bytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        catch (WebSocketException e)
        {
            _webSocket.Dispose();
        }
    }

    private async Task ReceiveMessages(string trainNumber)
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
                // サーバーからの切断要求を受けた場合
                return;
            }

            messageBytes.AddRange(buffer.Take(result.Count));
        } while (!result.EndOfMessage);

        // データが揃ったら文字列へエンコード
        var jsonResponse = _encoding.GetString(messageBytes.ToArray());
        messageBytes.Clear();

        // JSONレスポンスをデシリアライズ
        var traincrewBaseData = JsonSerializer.Deserialize<TraincrewBaseData>(jsonResponse);

        if (traincrewBaseData == null)
        {
            return;
        }

        // Typeプロパティに応じて処理
        if (traincrewBaseData.type != "TrainCrewStateData")
        {
            return;
        }

        // traincrewBaseData.dataをTrainCrewStateData型にデシリアライズ
        var dataJsonElement = (JsonElement)traincrewBaseData.data;
        var trainCrewStateData = JsonSerializer.Deserialize<TrainCrewStateData>(dataJsonElement.GetRawText());

        if (trainCrewStateData == null)
        {
            return;
        }

        _trackCircuitSet = trainCrewStateData
            .trackCircuitList
            .Where(trackCircuit => trackCircuit.Last == trainNumber)
            .Select(trackCircuit => trackCircuit.Name)
            .ToHashSet();
    }

    public TrainState GetTrainState()
    {
        if (_trainState == null)
        {
            throw new NullReferenceException(
                "Train crew state is null. Did you forget to call TrainCrewRepository.Fetch()?");
        }

        return _trainState;
    }

    public HashSet<string> GetTrackCircuitSet()
    {
        return _trackCircuitSet.ToHashSet();
    }

    public List<SignalInfo> GetSignalInfos()
    {
        return TrainCrewInput.signals.ToList();
    }

    public CrewType GetCrewType()
    {
        return TrainCrewInput.gameState.crewType;
    }

    public GameScreen GetGameScreen()
    {
        return TrainCrewInput.gameState.gameScreen;
    }
}