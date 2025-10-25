using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Domain.Interfaces;

/// <summary>
///     メロディー制御サービスのインターフェース
/// </summary>
public interface IMelodyControlService
{
    /// <summary>
    ///     メロディー再生を開始
    /// </summary>
    Task StartMelodyAsync();

    /// <summary>
    ///     メロディーを停止し、ドア閉め案内を再生
    /// </summary>
    Task StopMelodyAsync();

    /// <summary>
    ///     現在のメロディー状態を取得
    /// </summary>
    MelodyState GetCurrentState();

    /// <summary>
    ///     UI操作が有効かどうか(駅に在線しているか、一時停止中でないか)
    /// </summary>
    Task<bool> IsUiEnabledAsync();

    /// <summary>
    ///     メロディー状態が変化したときのイベント
    /// </summary>
    event EventHandler<MelodyState>? StateChanged;
}