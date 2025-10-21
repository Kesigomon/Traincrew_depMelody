using Traincrew_depMelody.Models;
using Traincrew_depMelody.Repository;

namespace Traincrew_depMelody.Service;

public class MelodyPathService(
    ITraincrewRepository traincrewRepository)
{
    private static Uri GetMelodyPath(string stationName, int trackNumber, bool isUp)
    {
        var defaultPath = Environment.CurrentDirectory + @"\sound\default.wav";
        var path = Environment.CurrentDirectory + @"\sound\" + stationName + "_" + trackNumber + ".wav";
        var result = System.IO.File.Exists(path) ? path : defaultPath;
        return new(result);
    }

    private static Uri GetDoorClosingPath(string stationName, int trackNumber, bool isUp)
    {
        return new(Environment.CurrentDirectory + @"\sound\doorClosing_" + trackNumber + ".wav");
    }

    /// <summary>
    /// 駅と番線の情報に基づいて音源ファイルのパスを決定する
    /// </summary>
    /// <param name="trackInfo">駅名と番線のペア</param>
    /// <param name="isMelodyPlaying">メロディ再生中かどうか</param>
    /// <returns>音源ファイルのURI</returns>
    public Uri GetAudioPath((string station, int platform) trackInfo, bool isMelodyPlaying)
    {
        var (station, platform) = trackInfo;
        var state = traincrewRepository.GetTrainState();
        // Todo: あとで
        var isUp = false;
        // 館浜の場合、種別により特殊な処理を入れる
        if (station == "館浜" && state.Class != "特急")
        {
            platform += 1;
        }
        return isMelodyPlaying ? GetMelodyPath(station, platform, isUp) : GetDoorClosingPath(station, platform, isUp);
    }
}