using Microsoft.Extensions.Logging;
using System.Windows.Media;
using Traincrew_depMelody.Domain.Interfaces.Services;

namespace Traincrew_depMelody.Infrastructure.AudioServices;

public class MediaPlayerService : IAudioPlayerService, IDisposable
{
    private readonly MediaPlayer _player;
    private readonly ILogger<MediaPlayerService> _logger;
    private bool _isLooping;
    private string? _currentFilePath;

    public bool IsPlaying { get; private set; }

    public double Volume
    {
        get => _player.Volume;
        set => _player.Volume = Math.Clamp(value, 0.0, 1.0);
    }

    public MediaPlayerService(ILogger<MediaPlayerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _player = new MediaPlayer();
        _player.MediaEnded += OnMediaEnded;
    }

    /// <summary>
    /// 音声をループ再生
    /// </summary>
    public async Task PlayLoopAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError($"音声ファイルが見つかりません: {filePath}");
            throw new FileNotFoundException("音声ファイルが見つかりません", filePath);
        }

        _currentFilePath = filePath;
        _isLooping = true;

        _player.Open(new Uri(filePath, UriKind.Absolute));
        _player.Play();

        IsPlaying = true;
        _logger.LogDebug($"ループ再生開始: {filePath}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 音声を1回だけ再生
    /// </summary>
    public async Task PlayOnceAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError($"音声ファイルが見つかりません: {filePath}");
            throw new FileNotFoundException("音声ファイルが見つかりません", filePath);
        }

        _currentFilePath = filePath;
        _isLooping = false;

        _player.Open(new Uri(filePath, UriKind.Absolute));
        _player.Play();

        IsPlaying = true;
        _logger.LogDebug($"1回再生開始: {filePath}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 再生を停止
    /// </summary>
    public void Stop()
    {
        _player.Stop();
        IsPlaying = false;
        _isLooping = false;
        _logger.LogDebug("再生停止");
    }

    /// <summary>
    /// 一時停止
    /// </summary>
    public void Pause()
    {
        _player.Pause();
        _logger.LogDebug("一時停止");
    }

    /// <summary>
    /// 一時停止から再開
    /// </summary>
    public void Resume()
    {
        _player.Play();
        _logger.LogDebug("再開");
    }

    /// <summary>
    /// メディア再生終了時の処理
    /// </summary>
    private void OnMediaEnded(object? sender, EventArgs e)
    {
        if (_isLooping && _currentFilePath != null)
        {
            // ループ再生
            _player.Position = TimeSpan.Zero;
            _player.Play();
            _logger.LogTrace("ループ再生継続");
        }
        else
        {
            IsPlaying = false;
            _logger.LogDebug("再生終了");
        }
    }

    public void Dispose()
    {
        _player.MediaEnded -= OnMediaEnded;
        _player.Close();
    }
}
