using System.Text;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Infrastructure.Repositories;

public class AudioProfileRepository : IAudioProfileRepository
{
    private readonly object _cacheLock = new();
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private readonly AppConfiguration _config;
    private readonly ILogger<AudioProfileRepository> _logger;
    private Dictionary<string, AudioProfile> _profiles = new();

    /// <summary>
    ///     現在のプロファイル名(拡張子なし)
    /// </summary>
    public string CurrentProfileName => _config.CurrentProfileName;

    public AudioProfileRepository(AppConfiguration config, ILogger<AudioProfileRepository> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     全ての音声プロファイルを取得
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
    ///     駅名と番線から音声プロファイルを検索
    /// </summary>
    public async Task<AudioProfile?> FindProfileAsync(string stationName, string trackNumber)
    {
        await EnsureLoadedAsync();

        var key = $"{stationName}_{trackNumber}";

        lock (_cacheLock)
        {
            return _profiles.ContainsKey(key) ? _profiles[key] : null;
        }
    }

    /// <summary>
    ///     音声プロファイルCSVを再読み込み
    /// </summary>
    public async Task ReloadAsync()
    {
        _logger.LogInformation("プロファイルCSVを再読み込み");
        await LoadCsvAsync(force: true);
    }

    /// <summary>
    ///     利用可能なプロファイル名を列挙(拡張子なし)
    /// </summary>
    public IEnumerable<string> GetAvailableProfileNames()
    {
        if (!Directory.Exists(_config.ProfilesDirectory))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.EnumerateFiles(_config.ProfilesDirectory, "*.csv")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .Cast<string>();
    }

    /// <summary>
    ///     プロファイルを切り替えてリロード
    /// </summary>
    public async Task SwitchProfileAsync(string profileName)
    {
        var csvPath = Path.Combine(_config.ProfilesDirectory, $"{profileName}.csv");

        if (!File.Exists(csvPath))
        {
            _logger.LogError("切り替え先プロファイルが見つかりません: {CsvPath}", csvPath);
            throw new FileNotFoundException("切り替え先プロファイルが見つかりません", csvPath);
        }

        var previous = _config.CurrentProfileName;
        _config.CurrentProfileName = profileName;
        try
        {
            await LoadCsvAsync(force: true);
        }
        catch
        {
            _config.CurrentProfileName = previous;
            throw;
        }

        _logger.LogInformation("プロファイルを切り替え: {ProfileName}", profileName);
    }

    /// <summary>
    ///     初回読み込み確認
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        lock (_cacheLock)
        {
            if (_profiles.Count > 0) return;
        }

        await LoadCsvAsync(force: false);
    }

    /// <summary>
    ///     CSVファイルを読み込み
    /// </summary>
    /// <param name="force">true の場合はキャッシュ済みでも強制リロード(ReloadAsync/SwitchProfileAsync から呼ぶ)</param>
    private async Task LoadCsvAsync(bool force = false)
    {
        await _loadSemaphore.WaitAsync();
        try
        {
            // force=false(EnsureLoadedAsync 経由)のとき、待機中に別スレッドが読み込み済みなら早期 return
            if (!force)
            {
                lock (_cacheLock)
                {
                    if (_profiles.Count > 0) return;
                }
            }

            var csvPath = Path.Combine(_config.ProfilesDirectory, $"{_config.CurrentProfileName}.csv");

            if (!File.Exists(csvPath))
            {
                _logger.LogError("プロファイルCSVが見つかりません: {CsvPath}", csvPath);
                throw new FileNotFoundException("プロファイルCSVが見つかりません", csvPath);
            }

            var profiles = new Dictionary<string, AudioProfile>();

            using var reader = new StreamReader(csvPath, Encoding.UTF8);

            // ヘッダー行をスキップ
            await reader.ReadLineAsync();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(',');

                if (values.Length < 4)
                {
                    _logger.LogWarning("不正なCSV行をスキップ: {Line}", line);
                    continue;
                }

                var stationName = values[0].Trim();
                var trackNumber = values[1].Trim();
                var melodyDownPath = values[2].Trim();
                var melodyUpPath = values[3].Trim();
                var doorDownPath = values.Length > 4 ? values[4].Trim() : null;
                var doorUpPath = values.Length > 5 ? values[5].Trim() : null;

                // 空文字列はnullに変換
                if (string.IsNullOrEmpty(doorDownPath)) doorDownPath = null;
                if (string.IsNullOrEmpty(doorUpPath)) doorUpPath = null;

                var profile = new AudioProfile(stationName, trackNumber, melodyDownPath, melodyUpPath, doorDownPath, doorUpPath);
                profiles[profile.GetKey()] = profile;
            }

            lock (_cacheLock)
            {
                _profiles = profiles;
            }

            _logger.LogInformation("プロファイルCSVを読み込みました: {ProfilesCount}件", profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "プロファイルCSV読み込み中にエラーが発生");
            throw;
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }
}