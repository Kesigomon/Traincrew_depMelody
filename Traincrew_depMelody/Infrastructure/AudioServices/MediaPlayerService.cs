using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Domain.Interfaces.Services;

namespace Traincrew_depMelody.Infrastructure.AudioServices;

public class MediaPlayerService : IAudioPlayerService, IDisposable
{
    private readonly ILogger<MediaPlayerService> _logger;
    private readonly MediaPlayer _player;
    private string? _currentFilePath;
    private bool _isLooping;

    public MediaPlayerService(ILogger<MediaPlayerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            MediaPlayer? created = null;
            dispatcher.Invoke(() =>
            {
                created = new MediaPlayer();
                created.MediaEnded += OnMediaEnded;
            });
            _player = created!;
        }
        else
        {
            _player = new MediaPlayer();
            _player.MediaEnded += OnMediaEnded;
        }
    }

    public bool IsPlaying { get; private set; }

    public double Volume
    {
        get => InvokeOnPlayerThread(() => _player.Volume);
        set => InvokeOnPlayerThread(() => _player.Volume = Math.Clamp(value, 0.0, 1.0));
    }

    private void InvokeOnPlayerThread(Action action)
    {
        var dispatcher = _player.Dispatcher;
        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }

    private T InvokeOnPlayerThread<T>(Func<T> func)
    {
        var dispatcher = _player.Dispatcher;
        if (dispatcher.CheckAccess())
            return func();
        else
            return (T)dispatcher.Invoke(func);
    }

    /// <summary>
    ///     音声をループ再生
    /// </summary>
    public async Task PlayLoopAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("音声ファイルが見つかりません: {FilePath}", filePath);
            throw new FileNotFoundException("音声ファイルが見つかりません", filePath);
        }

        _currentFilePath = filePath;
        _isLooping = true;

        try
        {
            InvokeOnPlayerThread(() =>
            {
                _player.Open(new Uri(filePath, UriKind.RelativeOrAbsolute));
                _player.Play();
            });
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "URI変換失敗: {FilePath}", filePath);
            throw;
        }

        IsPlaying = true;
        _logger.LogDebug("ループ再生開始: {FilePath}", filePath);

        await Task.CompletedTask;
    }

    /// <summary>
    ///     音声を1回だけ再生
    /// </summary>
    public async Task PlayOnceAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("音声ファイルが見つかりません: {FilePath}", filePath);
            throw new FileNotFoundException("音声ファイルが見つかりません", filePath);
        }

        _currentFilePath = filePath;
        _isLooping = false;

        try
        {
            InvokeOnPlayerThread(() =>
            {
                _player.Open(new Uri(filePath, UriKind.RelativeOrAbsolute));
                _player.Play();
            });
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "URI変換失敗: {FilePath}", filePath);
            throw;
        }

        IsPlaying = true;
        _logger.LogDebug("1回再生開始: {FilePath}", filePath);

        await Task.CompletedTask;
    }

    /// <summary>
    ///     再生を停止
    /// </summary>
    public void Stop()
    {
        InvokeOnPlayerThread(() => _player.Stop());
        IsPlaying = false;
        _isLooping = false;
        _logger.LogDebug("再生停止");
    }

    /// <summary>
    ///     一時停止
    /// </summary>
    public void Pause()
    {
        InvokeOnPlayerThread(() => _player.Pause());
        _logger.LogDebug("一時停止");
    }

    /// <summary>
    ///     一時停止から再開
    /// </summary>
    public void Resume()
    {
        InvokeOnPlayerThread(() => _player.Play());
        _logger.LogDebug("再開");
    }

    public void Dispose()
    {
        try
        {
            InvokeOnPlayerThread(() =>
            {
                _player.MediaEnded -= OnMediaEnded;
                _player.Close();
            });
        }
        catch (TaskCanceledException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    ///     メディア再生終了時の処理
    /// </summary>
    private void OnMediaEnded(object? sender, EventArgs e)
    {
        if (_isLooping && _currentFilePath != null)
        {
            // ループ再生
            InvokeOnPlayerThread(() =>
            {
                _player.Position = TimeSpan.Zero;
                _player.Play();
            });
            _logger.LogTrace("ループ再生継続");
        }
        else
        {
            IsPlaying = false;
            _logger.LogDebug("再生終了");
        }
    }
}