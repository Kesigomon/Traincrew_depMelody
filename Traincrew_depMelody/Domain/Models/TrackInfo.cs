namespace Traincrew_depMelody.Domain.Models;

/// <summary>
/// 駅・番線情報
/// </summary>
public class TrackInfo
{
    /// <summary>
    /// 駅名
    /// </summary>
    public string StationName { get; init; }

    /// <summary>
    /// 番線(例: "1", "2")
    /// </summary>
    public string TrackNumber { get; init; }

    /// <summary>
    /// この番線に対応する軌道回路IDのリスト
    /// </summary>
    public List<string> CircuitIds { get; init; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public TrackInfo(string stationName, string trackNumber, List<string> circuitIds)
    {
        StationName = stationName ?? throw new ArgumentNullException(nameof(stationName));
        TrackNumber = trackNumber ?? throw new ArgumentNullException(nameof(trackNumber));
        CircuitIds = circuitIds ?? throw new ArgumentNullException(nameof(circuitIds));
    }

    /// <summary>
    /// 指定された軌道回路IDがこの番線に該当するか判定
    /// </summary>
    public bool ContainsCircuit(string circuitId)
    {
        return CircuitIds.Contains(circuitId);
    }

    /// <summary>
    /// 駅・番線の識別キーを取得(例: "館浜_1")
    /// </summary>
    public string GetKey()
    {
        return $"{StationName}_{TrackNumber}";
    }
}
