namespace Traincrew_depMelody.Domain.Interfaces.Services;

/// <summary>
/// FFmpegサービスのインターフェース
/// </summary>
public interface IFFmpegService
{
    /// <summary>
    /// 音声ファイルの長さ(秒)を取得
    /// </summary>
    /// <param name="filePath">音声ファイルパス</param>
    /// <returns>音声の長さ(秒)、取得失敗時はnull</returns>
    Task<double?> GetAudioDurationAsync(string filePath);
}
