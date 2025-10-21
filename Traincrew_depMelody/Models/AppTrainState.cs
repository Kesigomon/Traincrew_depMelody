namespace Traincrew_depMelody.Models;

/// <summary>
/// アプリケーション独自の電車状態クラス
/// TrainCrewInput.TrainStateの代わりに使用
/// </summary>
public class AppTrainState
{
    /// <summary>
    /// 列車種別（例: "特急", "普通"）
    /// </summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>
    /// 全てのドアが閉まっているかどうか
    /// </summary>
    public bool AllClose { get; set; }

    /// <summary>
    /// 現在時刻（シミュレーション時刻）
    /// </summary>
    public TimeSpan NowTime { get; set; }

    /// <summary>
    /// 車両状態のリスト
    /// </summary>
    public List<AppCarState> CarStates { get; set; } = new();

    /// <summary>
    /// 駅リスト
    /// </summary>
    public List<AppStationInfo> StationList { get; set; } = new();

    /// <summary>
    /// 現在駅のインデックス
    /// </summary>
    public int NowStaIndex { get; set; }

    /// <summary>
    /// ダイヤ名/列車番号
    /// </summary>
    public string DiaName { get; set; } = string.Empty;

    /// <summary>
    /// 次駅名
    /// </summary>
    public string NextStaName { get; set; } = string.Empty;

    /// <summary>
    /// TrainCrewInput.TrainStateからAppTrainStateへ変換
    /// </summary>
    public static AppTrainState FromTrainCrewState(dynamic trainState)
    {
        var appState = new AppTrainState
        {
            Class = trainState.Class ?? string.Empty,
            AllClose = trainState.AllClose,
            NowTime = trainState.NowTime,
            DiaName = trainState.diaName ?? string.Empty,
            NextStaName = trainState.nextStaName ?? string.Empty,
            NowStaIndex = trainState.nowStaIndex,
            CarStates = new List<AppCarState>(),
            StationList = new List<AppStationInfo>()
        };

        // CarStatesの変換
        if (trainState.CarStates != null)
        {
            foreach (var carState in trainState.CarStates)
            {
                appState.CarStates.Add(new AppCarState
                {
                    CarModel = carState.CarModel ?? string.Empty
                });
            }
        }

        // StationListの変換
        if (trainState.stationList != null)
        {
            foreach (var station in trainState.stationList)
            {
                appState.StationList.Add(new AppStationInfo
                {
                    DepTime = station.DepTime
                });
            }
        }

        return appState;
    }
}

/// <summary>
/// 車両状態
/// </summary>
public class AppCarState
{
    /// <summary>
    /// 車両モデル（例: "50000"）
    /// </summary>
    public string CarModel { get; set; } = string.Empty;
}

/// <summary>
/// 駅情報
/// </summary>
public class AppStationInfo
{
    /// <summary>
    /// 発車時刻
    /// </summary>
    public TimeSpan DepTime { get; set; }
}
