using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Domain.Interfaces.Services;

/// <summary>
///     Traincrewゲーム連携サービスのインターフェース
/// </summary>
public interface ITraincrewGameService
{
    /// <summary>
    ///     接続中かどうか
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     ゲームに接続
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    ///     ゲームから切断
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    ///     現在のゲーム状態を取得
    /// </summary>
    Task<GameState> GetCurrentGameStateAsync();

    /// <summary>
    ///     ゲーム状態が変化したときのイベント
    /// </summary>
    event EventHandler<GameState>? GameStateChanged;
}