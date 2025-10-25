namespace Traincrew_depMelody.Domain.Interfaces.Services;

/// <summary>
///     音声再生サービスのインターフェース
/// </summary>
public interface IAudioPlayerService
{
    /// <summary>
    ///     再生中かどうか
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    ///     音量(0.0 ～ 1.0)
    /// </summary>
    double Volume { get; set; }

    /// <summary>
    ///     音声をループ再生
    /// </summary>
    Task PlayLoopAsync(string filePath);

    /// <summary>
    ///     音声を1回だけ再生
    /// </summary>
    Task PlayOnceAsync(string filePath);

    /// <summary>
    ///     再生を停止
    /// </summary>
    void Stop();

    /// <summary>
    ///     一時停止
    /// </summary>
    void Pause();

    /// <summary>
    ///     一時停止から再開
    /// </summary>
    void Resume();
}