namespace Traincrew_depMelody.Domain.Models;

/// <summary>
/// 自動モード設定
/// </summary>
public record AutoModeConfig
{
    /// <summary>
    /// 自動モードが有効か
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// 到着後の待機時間(秒)
    /// </summary>
    public double DelayAfterArrival { get; init; } = 1.0;

    /// <summary>
    /// 信号開通後の待機時間(秒)
    /// </summary>
    public double DelayAfterSignalOpen { get; init; } = 0.5;

    /// <summary>
    /// メロディーON後の最低待機時間(秒)
    /// </summary>
    public double MinimumMelodyDuration { get; init; } = 1.0;

    /// <summary>
    /// ドア開後の最低待機時間(秒)
    /// </summary>
    public double MinimumDoorOpenDuration { get; init; } = 12.0;

    /// <summary>
    /// ドア閉め案内の再生時間(秒)
    /// </summary>
    public double DoorCloseAnnouncementDuration { get; init; } = 3.0;

    /// <summary>
    /// 通常車両のマージン(秒)
    /// </summary>
    public double StandardMargin { get; init; } = 8.5;

    /// <summary>
    /// 50000系のマージン(秒)
    /// </summary>
    public double Series50000Margin { get; init; } = 16.5;

    /// <summary>
    /// 車両形式に応じたマージンを取得
    /// </summary>
    public double GetMarginForVehicle(TrainState trainState)
    {
        return trainState.IsLimitedExpressType() ? Series50000Margin : StandardMargin;
    }
}
