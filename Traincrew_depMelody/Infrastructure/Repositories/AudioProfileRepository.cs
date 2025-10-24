using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Infrastructure.Repositories;

public class AudioProfileRepository : IAudioProfileRepository
{
    private readonly AppConfiguration _config;
    private readonly ILogger<AudioProfileRepository> _logger;
    private Dictionary<string, AudioProfile> _profiles = new Dictionary<string, AudioProfile>();
    private readonly object _cacheLock = new object();

    public AudioProfileRepository(AppConfiguration config, ILogger<AudioProfileRepository> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 全ての音声プロファイルを取得
    /// </summary>
    public async Task<IEnumerable<AudioProfile>> GetAllProfilesAsync()
    {
        await EnsureLoadedAsync();

        lock (_cacheLock)
        {
            return _profiles.Values.ToList();
        }
    }

    /// <summary>
    /// 駅名と番線から音声プロファイルを検索
    /// </summary>
    public async Task<AudioProfile?> FindProfileAsync(string stationName, string trackNumber)
    {
        await EnsureLoadedAsync();

        string key = $"{stationName}_{trackNumber}";

        lock (_cacheLock)
        {
            return _profiles.ContainsKey(key) ? _profiles[key] : null;
        }
    }

    /// <summary>
    /// 音声プロファイルCSVを再読み込み
    /// </summary>
    public async Task ReloadAsync()
    {
        _logger.LogInformation("プロファイルCSVを再読み込み");
        await LoadCsvAsync();
    }

    /// <summary>
    /// 初回読み込み確認
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        lock (_cacheLock)
        {
            if (_profiles.Count > 0) return;
        }

        await LoadCsvAsync();
    }

    /// <summary>
    /// CSVファイルを読み込み
    /// </summary>
    private async Task LoadCsvAsync()
    {
        string csvPath = Path.Combine(_config.ProfilesDirectory, $"{_config.CurrentProfileName}.csv");

        if (!File.Exists(csvPath))
        {
            _logger.LogError($"プロファイルCSVが見つかりません: {csvPath}");
            throw new FileNotFoundException("プロファイルCSVが見つかりません", csvPath);
        }

        var profiles = new Dictionary<string, AudioProfile>();

        try
        {
            using var reader = new StreamReader(csvPath, System.Text.Encoding.UTF8);

            // ヘッダー行をスキップ
            await reader.ReadLineAsync();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(',');

                if (values.Length < 3)
                {
                    _logger.LogWarning($"不正なCSV行をスキップ: {line}");
                    continue;
                }

                string stationName = values[0].Trim();
                string trackNumber = values[1].Trim();
                string melodyPath = values[2].Trim();
                string? doorDownPath = values.Length > 3 ? values[3].Trim() : null;
                string? doorUpPath = values.Length > 4 ? values[4].Trim() : null;

                // 空文字列はnullに変換
                if (string.IsNullOrEmpty(doorDownPath)) doorDownPath = null;
                if (string.IsNullOrEmpty(doorUpPath)) doorUpPath = null;

                var profile = new AudioProfile(stationName, trackNumber, melodyPath, doorDownPath, doorUpPath);
                profiles[profile.GetKey()] = profile;
            }

            lock (_cacheLock)
            {
                _profiles = profiles;
            }

            _logger.LogInformation($"プロファイルCSVを読み込みました: {profiles.Count}件");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "プロファイルCSV読み込み中にエラーが発生");
            throw;
        }
    }
}
