namespace Traincrew_depMelody.Domain.Interfaces.Services;

/// <summary>
///     設定永続化サービスのインターフェース
/// </summary>
public interface IConfigurationPersistenceService
{
    /// <summary>
    ///     プロファイル名を appsettings.json に保存
    /// </summary>
    Task SaveProfileNameAsync(string profileName);
}
