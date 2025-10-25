namespace Traincrew_depMelody.Domain.Models;

/// <summary>
/// メロディー再生状態
/// </summary>
public class MelodyState
{
    /// <summary>
    /// 再生中かどうか
    /// </summary>
    public bool IsPlaying { get; init; }

    /// <summary>
    /// 現在再生中のメロディーファイルパス
    /// </summary>
    public string? CurrentMelodyPath { get; init; }

    /// <summary>
    /// 現在の駅・番線情報
    /// </summary>
    public TrackInfo? CurrentTrack { get; init; }

    /// <summary>
    /// メロディー開始時刻 (ゲーム内時刻)
    /// </summary>
    public TimeSpan? StartedAt { get; init; }

    /// <summary>
    /// ドア閉め案内を再生済みか
    /// </summary>
    public bool DoorCloseAnnouncementPlayed { get; init; }

    /// <summary>
    /// 新しい状態を生成(イミュータブル更新)
    /// </summary>
    public MelodyState With(
        bool? isPlaying = null,
        string? currentMelodyPath = null,
        TrackInfo? currentTrack = null,
        TimeSpan? startedAt = null,
        bool? doorCloseAnnouncementPlayed = null)
    {
        return new MelodyState
        {
            IsPlaying = isPlaying ?? IsPlaying,
            CurrentMelodyPath = currentMelodyPath ?? CurrentMelodyPath,
            CurrentTrack = currentTrack ?? CurrentTrack,
            StartedAt = startedAt ?? StartedAt,
            DoorCloseAnnouncementPlayed = doorCloseAnnouncementPlayed ?? DoorCloseAnnouncementPlayed
        };
    }
}
