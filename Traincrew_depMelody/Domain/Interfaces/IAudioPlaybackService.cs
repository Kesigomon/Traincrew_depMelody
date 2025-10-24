using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Domain.Interfaces;

/// <summary>
/// 音声再生統合サービスのインターフェース
/// </summary>
public interface IAudioPlaybackService
{
    /// <summary>
    /// メロディーを再生(ループ)
    /// </summary>
    Task PlayMelodyAsync(TrackInfo track);

    /// <summary>
    /// メロディーを停止
    /// </summary>
    void StopMelody();

    /// <summary>
    /// ドア閉め案内を再生(1回)
    /// </summary>
    Task PlayDoorCloseAnnouncementAsync(TrackInfo track, bool isInbound);

    /// <summary>
    /// 全ての音声を一時停止
    /// </summary>
    void PauseAll();

    /// <summary>
    /// 一時停止から再開
    /// </summary>
    void ResumeAll();

    /// <summary>
    /// メロディーの長さを取得(秒)
    /// </summary>
    Task<double> GetMelodyDurationAsync(TrackInfo track);
}
