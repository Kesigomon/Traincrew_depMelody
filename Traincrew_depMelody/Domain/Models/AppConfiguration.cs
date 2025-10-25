namespace Traincrew_depMelody.Domain.Models;

/// <summary>
///     アプリケーション設定
/// </summary>
public class AppConfiguration
{
    /// <summary>
    ///     FFmpegの実行ファイルパス
    /// </summary>
    public string FFmpegPath { get; set; } = @"C:\ffmpeg\bin\ffmpeg.exe";

    /// <summary>
    ///     音声ファイルのベースディレクトリ
    /// </summary>
    public string AudioBaseDirectory { get; set; } = @".\Audio";

    /// <summary>
    ///     メロディーフォルダパス
    /// </summary>
    public string MelodyDirectory => Path.Combine(AudioBaseDirectory, "Melody");

    /// <summary>
    ///     案内音声フォルダパス
    /// </summary>
    public string AnnouncementDirectory => Path.Combine(AudioBaseDirectory, "Announcement");

    /// <summary>
    ///     stations.csvのパス
    /// </summary>
    public string StationsCsvPath { get; set; } = @".\stations\stations.csv";

    /// <summary>
    ///     プロファイルディレクトリのパス
    /// </summary>
    public string ProfilesDirectory { get; set; } = @".\profiles";

    /// <summary>
    ///     使用するプロファイル名(拡張子なし)
    /// </summary>
    public string CurrentProfileName { get; set; } = "default";

    /// <summary>
    ///     WebSocketポート番号
    /// </summary>
    public int WebSocketPort { get; set; } = 50300;

    /// <summary>
    ///     ゲーム状態ポーリング間隔(ミリ秒)
    /// </summary>
    public int GameStatePollingIntervalMs { get; set; } = 16;

    /// <summary>
    ///     デフォルトメロディーファイル名
    /// </summary>
    public string DefaultMelodyFileName { get; set; } = "default.mp3";

    /// <summary>
    ///     ログ出力ディレクトリ
    /// </summary>
    public string LogDirectory { get; set; } = @".\Logs";

    /// <summary>
    ///     開発者モード(デバッグ情報表示)
    /// </summary>
    public bool DeveloperMode { get; set; } = false;
}