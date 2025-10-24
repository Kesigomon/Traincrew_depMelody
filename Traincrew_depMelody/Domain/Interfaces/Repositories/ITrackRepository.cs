using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Domain.Interfaces.Repositories;

/// <summary>
/// 駅・番線情報リポジトリのインターフェース
/// </summary>
public interface ITrackRepository
{
    /// <summary>
    /// 全ての駅・番線情報を取得
    /// </summary>
    Task<IEnumerable<TrackInfo>> GetAllTracksAsync();

    /// <summary>
    /// 軌道回路IDから駅・番線情報を検索
    /// </summary>
    /// <param name="circuitId">軌道回路ID</param>
    /// <returns>該当する駅・番線情報、見つからない場合はnull</returns>
    Task<TrackInfo?> FindTrackByCircuitIdAsync(string circuitId);

    /// <summary>
    /// 駅名と番線から駅・番線情報を検索
    /// </summary>
    Task<TrackInfo?> FindTrackByStationAndNumberAsync(string stationName, string trackNumber);

    /// <summary>
    /// CSVを再読み込み(キャッシュクリア)
    /// </summary>
    Task ReloadAsync();
}
