using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Domain.Interfaces;

/// <summary>
/// 自動モードサービスのインターフェース
/// </summary>
public interface IAutoModeService
{
    /// <summary>
    /// 自動モードを開始
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 自動モードを停止
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 自動モードが有効かどうか
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 自動モード設定を取得
    /// </summary>
    AutoModeConfig GetConfig();

    /// <summary>
    /// 自動モード設定を更新
    /// </summary>
    void UpdateConfig(AutoModeConfig config);
}
