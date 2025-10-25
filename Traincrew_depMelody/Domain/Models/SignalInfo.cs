namespace Traincrew_depMelody.Domain.Models;

/// <summary>
///     信号情報
/// </summary>
public class SignalInfo
{
    /// <summary>
    ///     信号現示
    /// </summary>
    public SignalAspect Aspect { get; init; }

    /// <summary>
    ///     信号が開通しているか
    /// </summary>
    public bool IsOpen => Aspect != SignalAspect.Stop;

    /// <summary>
    ///     信号が開通した時刻(状態遷移検知用)
    /// </summary>
    public DateTime? OpenedAt { get; init; }
}

/// <summary>
///     信号現示
/// </summary>
public enum SignalAspect
{
    Stop, // 停止
    Proceed // 進行(進行を指示する全ての現示を含む)
}