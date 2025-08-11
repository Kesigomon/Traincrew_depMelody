using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Traincrew_depMelody.Repository;

public interface IFFmpegRepository
{
    Task<double> GetDuration(Uri uri);
}

public partial class FFmpegRepository: IFFmpegRepository
{
    const string ffmpegPath = "ffmpeg.exe";
    public async Task<double> GetDuration(Uri uri)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-i \"{uri.LocalPath}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        var output = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var match = MyRegex().Match(output);
        if (!match.Success)
        {
            return 0.0;
        }
        var hours = int.Parse(match.Groups[1].Value);
        var minutes = int.Parse(match.Groups[2].Value);
        var seconds = int.Parse(match.Groups[3].Value);
        var milliseconds = int.Parse(match.Groups[4].Value) * 10;

        return new TimeSpan(0, hours, minutes, seconds, milliseconds).TotalSeconds;
    }

    [GeneratedRegex(@"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})")]
    private static partial Regex MyRegex();
    
}