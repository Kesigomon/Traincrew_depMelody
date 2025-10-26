using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Infrastructure.ExternalServices;

public class FFmpegService : IFFmpegService
{
    private readonly AppConfiguration _config;
    private readonly ILogger<FFmpegService> _logger;

    public FFmpegService(AppConfiguration config, ILogger<FFmpegService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     音声ファイルの長さ(秒)を取得
    /// </summary>
    public async Task<double?> GetAudioDurationAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("音声ファイルが見つかりません: {FilePath}", filePath);
            return null;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _config.FFmpegPath,
                Arguments = $"-i \"{filePath}\" -hide_banner",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("FFmpegプロセスの起動に失敗");
                return null;
            }

            var output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // "Duration: 00:00:35.50" のような行をパース
            var match = Regex.Match(output, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");

            if (!match.Success)
            {
                _logger.LogWarning("音声ファイル長の取得に失敗: {FilePath}", filePath);
                return null;
            }

            var hours = int.Parse(match.Groups[1].Value);
            var minutes = int.Parse(match.Groups[2].Value);
            var seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            var totalSeconds = hours * 3600 + minutes * 60 + seconds;

            _logger.LogDebug("音声ファイル長: {TotalSeconds}秒 ({FilePath})", totalSeconds, filePath);
            return totalSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg実行中にエラーが発生: {FilePath}", filePath);
            return null;
        }
    }
}