using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Domain.Interfaces.Services;

/// <summary>
///     シリアルポート経由の物理ボタン入力サービスのインターフェース
/// </summary>
public interface ISerialButtonService
{
    /// <summary>
    ///     接続中かどうか
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     現在のボタン状態 (true=ON, false=OFF)
    /// </summary>
    bool CurrentState { get; }

    /// <summary>
    ///     ボタン状態が変化したときのイベント (true=ON, false=OFF)
    /// </summary>
    event EventHandler<bool>? ButtonStateChanged;

    /// <summary>
    ///     指定した設定でシリアルポートに接続する
    /// </summary>
    Task ConnectAsync(SerialButtonConfig config);

    /// <summary>
    ///     シリアルポートから切断する
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    ///     利用可能なシリアルポート一覧を返す
    /// </summary>
    IReadOnlyList<string> GetAvailablePorts();
}
