namespace Traincrew_depMelody.Domain.Models;

/// <summary>
/// 音声プロファイル(駅・番線ごとの音声設定)
/// </summary>
public class AudioProfile
{
    /// <summary>
    /// 駅名
    /// </summary>
    public string StationName { get; init; }

    /// <summary>
    /// 番線
    /// </summary>
    public string TrackNumber { get; init; }

    /// <summary>
    /// 発車メロディーファイルパス
    /// </summary>
    public string MelodyFilePath { get; init; }

    /// <summary>
    /// ドア閉め案内ファイルパス(下り)
    /// </summary>
    public string? DoorCloseAnnouncementDownFilePath { get; init; }

    /// <summary>
    /// ドア閉め案内ファイルパス(上り)
    /// </summary>
    public string? DoorCloseAnnouncementUpFilePath { get; init; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public AudioProfile(
        string stationName,
        string trackNumber,
        string melodyFilePath,
        string? doorCloseAnnouncementDownFilePath = null,
        string? doorCloseAnnouncementUpFilePath = null)
    {
        StationName = stationName ?? throw new ArgumentNullException(nameof(stationName));
        TrackNumber = trackNumber ?? throw new ArgumentNullException(nameof(trackNumber));
        MelodyFilePath = melodyFilePath ?? throw new ArgumentNullException(nameof(melodyFilePath));
        DoorCloseAnnouncementDownFilePath = doorCloseAnnouncementDownFilePath;
        DoorCloseAnnouncementUpFilePath = doorCloseAnnouncementUpFilePath;
    }

    /// <summary>
    /// 識別キーを取得(駅名_番線)
    /// </summary>
    public string GetKey()
    {
        return $"{StationName}_{TrackNumber}";
    }

    /// <summary>
    /// 指定方向のドア閉め案内ファイルパスを取得
    /// </summary>
    public string? GetDoorCloseAnnouncementPath(bool isInbound)
    {
        return isInbound ? DoorCloseAnnouncementUpFilePath : DoorCloseAnnouncementDownFilePath;
    }
}
