namespace Traincrew_depMelody.Domain.Models;

/// <summary>
/// ゲーム状態
/// </summary>
public class GameState
{
    /// <summary>
    /// ゲーム画面種別
    /// </summary>
    public GameScreen Screen { get; init; }

    /// <summary>
    /// 一時停止中かどうか
    /// </summary>
    public bool IsPaused { get; init; }

    /// <summary>
    /// 乗務員種別
    /// </summary>
    public CrewType CrewType { get; init; }

    /// <summary>
    /// 列車状態
    /// </summary>
    public TrainState? TrainState { get; init; }

    /// <summary>
    /// 信号情報
    /// </summary>
    public SignalInfo? SignalInfo { get; init; }

    /// <summary>
    /// 現在在線している軌道回路ID
    /// </summary>
    public string? CurrentCircuitId { get; init; }

    /// <summary>
    /// ゲーム内の現在時刻(ポーズに追従するため)
    /// </summary>
    public DateTime CurrentGameTime { get; init; }

    /// <summary>
    /// プレイ中かどうか
    /// </summary>
    public bool IsPlaying => Screen == GameScreen.Driving || Screen == GameScreen.Conducting;

    /// <summary>
    /// 駅に在線しているかどうか(軌道回路が駅ホームトラックか)
    /// </summary>
    public bool IsAtStation { get; init; }
}

/// <summary>
/// ゲーム画面種別
/// </summary>
public enum GameScreen
{
    Menu,
    Driving,
    Conducting,
    Other
}

/// <summary>
/// 乗務員種別
/// </summary>
public enum CrewType
{
    None,           // 非乗務
    Driver,         // 運転士(車掌同乗)
    DriverOnly,     // 運転士ワンマン
    Conductor       // 車掌
}
