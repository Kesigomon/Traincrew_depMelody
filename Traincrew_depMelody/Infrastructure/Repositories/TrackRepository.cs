using System.Text;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Infrastructure.Repositories;

public class TrackRepository : ITrackRepository
{
    private readonly object _cacheLock = new();
    private readonly AppConfiguration _config;
    private readonly ILogger<TrackRepository> _logger;
    private List<TrackInfo> _tracks = new();

    public TrackRepository(AppConfiguration config, ILogger<TrackRepository> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     軌道回路IDから駅・番線情報を検索
    /// </summary>
    public async Task<TrackInfo?> FindTrackByCircuitIdAsync(string circuitId)
    {
        await EnsureLoadedAsync();

        lock (_cacheLock)
        {
            return _tracks.FirstOrDefault(t => t.ContainsCircuit(circuitId));
        }
    }

    /// <summary>
    ///     CSVを再読み込み
    /// </summary>
    public async Task ReloadAsync()
    {
        _logger.LogInformation("stations.csvを再読み込み");
        await LoadCsvAsync();
    }

    /// <summary>
    ///     軌道回路IDのいずれかが駅ホームに存在するか判定
    /// </summary>
    public async Task<bool> IsAnyCircuitAtStationAsync(IEnumerable<string> circuitIds)
    {
        await EnsureLoadedAsync();

        lock (_cacheLock)
        {
            return circuitIds.Any(circuitId => _tracks.Any(t => t.ContainsCircuit(circuitId)));
        }
    }

    /// <summary>
    ///     初回読み込み確認
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        lock (_cacheLock)
        {
            if (_tracks.Count > 0) return;
        }

        await LoadCsvAsync();
    }

    /// <summary>
    ///     CSVファイルを読み込み
    /// </summary>
    private async Task LoadCsvAsync()
    {
        var csvPath = _config.StationsCsvPath;

        if (!File.Exists(csvPath))
        {
            _logger.LogError("stations.csvが見つかりません: {CsvPath}", csvPath);
            throw new FileNotFoundException("stations.csvが見つかりません", csvPath);
        }

        var tracks = new List<TrackInfo>();

        try
        {
            using var reader = new StreamReader(csvPath, Encoding.UTF8);

            // ヘッダー行をスキップ
            await reader.ReadLineAsync();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(',');

                if (values.Length < 3)
                {
                    _logger.LogWarning("不正なCSV行をスキップ: {Line}", line);
                    continue;
                }

                var stationName = values[0].Trim();
                var trackNumber = values[1].Trim();

                var circuits = new List<string>();
                for (var i = 2; i < values.Length; i++)
                {
                    var circuit = values[i].Trim();
                    if (!string.IsNullOrEmpty(circuit)) circuits.Add(circuit);
                }

                var trackInfo = new TrackInfo(stationName, trackNumber, circuits);
                tracks.Add(trackInfo);
            }

            lock (_cacheLock)
            {
                _tracks = tracks;
            }

            _logger.LogInformation("stations.csvを読み込みました: {TracksCount}件", tracks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "stations.csv読み込み中にエラーが発生");
            throw;
        }
    }
}