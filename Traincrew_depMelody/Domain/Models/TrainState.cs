namespace Traincrew_depMelody.Domain.Models;

/// <summary>
///     列車状態
/// </summary>
public class TrainState
{
    /// <summary>
    ///     速度 (km/h)
    /// </summary>
    public double Speed { get; init; }

    /// <summary>
    ///     ドアが開いているか
    /// </summary>
    public bool IsDoorsOpen { get; init; }

    /// <summary>
    ///     列車番号(例: "1206A")
    /// </summary>
    public string? TrainNumber { get; init; }

    /// <summary>
    ///     車両形式のリスト(例: ["50000", "50100"])
    /// </summary>
    public List<string> VehicleTypes { get; init; } = new();

    /// <summary>
    ///     発車時刻(ダイヤ上の予定時刻、ゲーム内時刻)
    /// </summary>
    public TimeSpan? DepartureTime { get; init; }

    /// <summary>
    ///     停車中かどうか
    /// </summary>
    public bool IsStopped => Speed < 0.1;

    /// <summary>
    ///     上り列車かどうかを判定(列車番号の末尾側から見て最初に現れる数字で判断)
    /// </summary>
    public bool IsInbound()
    {
        var lastDigit = TrainNumber?.LastOrDefault(char.IsDigit) ?? '\0';
        return char.IsDigit(lastDigit) && (lastDigit - '0') % 2 == 0;
    }

    /// <summary>
    ///     特急型車両かどうか
    /// </summary>
    public bool IsLimitedExpressType()
    {
        return VehicleTypes.Count > 0 && VehicleTypes[0].StartsWith("50000");
    }
}