using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Domain.Interfaces.Repositories;

/// <summary>
/// 音声プロファイルリポジトリのインターフェース
/// </summary>
public interface IAudioProfileRepository
{
    /// <summary>
    /// 全ての音声プロファイルを取得
    /// </summary>
    Task<IEnumerable<AudioProfile>> GetAllProfilesAsync();

    /// <summary>
    /// 駅名と番線から音声プロファイルを検索
    /// </summary>
    Task<AudioProfile?> FindProfileAsync(string stationName, string trackNumber);

    /// <summary>
    /// 音声プロファイルCSVを再読み込み
    /// </summary>
    Task ReloadAsync();
}
