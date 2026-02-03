namespace Traincrew_depMelody.Domain.Models;

/// <summary>
///     音声プロファイル(駅・番線ごとの音声設定)
/// </summary>
public class AudioProfile
{
    /// <summary>
    ///     コンストラクタ
    /// </summary>
    public AudioProfile(
        string stationName,
        string trackNumber,
        string melodyDownFilePath,
        string melodyUpFilePath,
        string? doorCloseAnnouncementDownFilePath = null,
        string? doorCloseAnnouncementUpFilePath = null)
    {
        StationName = stationName ?? throw new ArgumentNullException(nameof(stationName));
        TrackNumber = trackNumber ?? throw new ArgumentNullException(nameof(trackNumber));
        MelodyDownFilePath = melodyDownFilePath ?? throw new ArgumentNullException(nameof(melodyDownFilePath));
        MelodyUpFilePath = melodyUpFilePath ?? throw new ArgumentNullException(nameof(melodyUpFilePath));
        DoorCloseAnnouncementDownFilePath = doorCloseAnnouncementDownFilePath;
        DoorCloseAnnouncementUpFilePath = doorCloseAnnouncementUpFilePath;
    }

    /// <summary>
    ///     駅名
    /// </summary>
    public string StationName { get; init; }

    /// <summary>
    ///     番線
    /// </summary>
    public string TrackNumber { get; init; }

    /// <summary>
    ///     発車メロディーファイルパス(下り)
    /// </summary>
    public string MelodyDownFilePath { get; init; }

    /// <summary>
    ///     発車メロディーファイルパス(上り)
    /// </summary>
    public string MelodyUpFilePath { get; init; }

    /// <summary>
    ///     ドア閉め案内ファイルパス(下り)
    /// </summary>
    public string? DoorCloseAnnouncementDownFilePath { get; init; }

    /// <summary>
    ///     ドア閉め案内ファイルパス(上り)
    /// </summary>
    public string? DoorCloseAnnouncementUpFilePath { get; init; }

    /// <summary>
    ///     識別キーを取得(駅名_番線)
    /// </summary>
    public string GetKey()
    {
        return $"{StationName}_{TrackNumber}";
    }

    /// <summary>
    ///     指定方向のメロディーファイルパスを取得
    /// </summary>
    public string GetMelodyPath(bool isInbound)
    {
        return isInbound ? MelodyUpFilePath : MelodyDownFilePath;
    }

    /// <summary>
    ///     指定方向のドア閉め案内ファイルパスを取得
    /// </summary>
    public string? GetDoorCloseAnnouncementPath(bool isInbound)
    {
        return isInbound ? DoorCloseAnnouncementUpFilePath : DoorCloseAnnouncementDownFilePath;
    }
}