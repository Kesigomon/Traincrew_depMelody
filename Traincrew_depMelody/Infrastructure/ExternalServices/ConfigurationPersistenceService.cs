using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Infrastructure.ExternalServices;

/// <summary>
///     appsettings.json への設定永続化サービス
/// </summary>
public class ConfigurationPersistenceService : IConfigurationPersistenceService
{
    private readonly ILogger<ConfigurationPersistenceService> _logger;

    public ConfigurationPersistenceService(ILogger<ConfigurationPersistenceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     プロファイル名を appsettings.json に保存
    /// </summary>
    public async Task SaveProfileNameAsync(string profileName)
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        try
        {
            var json = await File.ReadAllTextAsync(settingsPath);
            var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("appsettings.json のパースに失敗しました");
            var obj = node.AsObject();
            obj["CurrentProfileName"] = profileName;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var newJson = obj.ToJsonString(options);
            await File.WriteAllTextAsync(settingsPath, newJson, new UTF8Encoding(false));

            _logger.LogInformation("プロファイル名を appsettings.json に保存しました: {ProfileName}", profileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "appsettings.json への書き込みに失敗しました");
            throw;
        }
    }

    /// <summary>
    ///     最前面表示モードを appsettings.json に保存
    /// </summary>
    public async Task SaveTopmostModeAsync(TopmostMode mode)
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        try
        {
            var json = await File.ReadAllTextAsync(settingsPath);
            var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("appsettings.json のパースに失敗しました");
            var obj = node.AsObject();
            obj["TopmostMode"] = mode.ToString();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var newJson = obj.ToJsonString(options);
            await File.WriteAllTextAsync(settingsPath, newJson, new UTF8Encoding(false));

            _logger.LogInformation("最前面表示モードを appsettings.json に保存しました: {TopmostMode}", mode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "appsettings.json への書き込みに失敗しました");
            throw;
        }
    }
}
