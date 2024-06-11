using System.Windows.Media;
using TrainCrew;

namespace Traincrew_depMelody;

public class AutoMode
{
    // Todo: 50000系使用時はマージンを長くする
    private const double MelodyDurationMargin = 8.5;
    private Action<bool> pushHandler;
    private bool isEnabled;
    private bool isPlaying;
    private TimeSpan pushOnTime;
    private TimeSpan pushOffTime;
    private double melodyDuration = 0.0;
    private double doorClosingDuration = 0.0;
    private int _previousTrackNumber = 0;

    public AutoMode(Action<bool> pushHandler)
    {
        this.pushHandler = pushHandler;
        Reset(false);
    }

    public void Reset(bool isEnabled)
    {
        this.isEnabled = isEnabled;
        isPlaying = false;
        _previousTrackNumber = 0;
        pushOnTime = TimeSpan.MaxValue;
        pushOffTime = TimeSpan.MaxValue;
        melodyDuration = 0.0;
        doorClosingDuration = 0.0;
    }

    public void Elapse(TrainState state, IEnumerable<SignalInfo> signals, int trackNumber)
    {
       _elapse(state, signals, trackNumber); 
       _previousTrackNumber = trackNumber;
    }
    
    private void _elapse(TrainState state, IEnumerable<SignalInfo> signals, int trackNumber)
    {
        if (!isEnabled || trackNumber <= 0)
        {
            return;
        }

        if (_previousTrackNumber <= 0)
        {
            onStoppedStation(state, trackNumber);
            return;
        }

        var nowTime = state.NowTime;
        if(nowTime < pushOnTime)
        {
            return;
        }
        
        // OFFを押す時間が来たらOFFを押す
        if (nowTime >= pushOffTime && isPlaying)
        {
            // OFFを押す
            PushOffButton();
            return;
        }
        if(isPlaying || !isOpened(signals))
        {
            return;
        }
        // ONを押す
        PushOnButton();
        // OFFを押す時間を計算
        var nowTimeSeconds = nowTime.TotalSeconds;
        var departureTimeSeconds = state.stationList[state.nowStaIndex].DepTime.TotalSeconds;
        var pushOffSeconds =
            new[]
            {
                nowTimeSeconds + 1.0,
                nowTimeSeconds + MelodyDurationMargin - doorClosingDuration,
                departureTimeSeconds - doorClosingDuration - MelodyDurationMargin + 0.5
            }.Max();
        pushOffTime = TimeSpan.FromSeconds(pushOffSeconds);
    }
    
    private async Task onStoppedStation(TrainState state, int trackNumber)
    {
        if (!isEnabled)
        {
            return;
        }

        double nowTime = state.NowTime.TotalSeconds;
        double departureTime = state.stationList[state.nowStaIndex].DepTime.TotalSeconds;
        melodyDuration = await GetMelodyLength(MainWindow.GetMelodyPath(state.nextStaName, trackNumber));
        doorClosingDuration = await GetMelodyLength(MainWindow.GetDoorClosingPath(state.nextStaName, trackNumber));
        double pushOnSeconds = 
            double.Max(
                // 電車到着後1秒後
                nowTime + 1.0,
                // 発車時刻 - メロディ時間 - ドア閉まる時間 - マージン(8.5秒)
                departureTime - melodyDuration - doorClosingDuration - MelodyDurationMargin
            );
        pushOnTime = TimeSpan.FromSeconds(pushOnSeconds);
    }
    
    private void Push(bool willPlaying)
    {
        
        isPlaying = willPlaying;
        pushHandler(willPlaying);
    }
    
    private void PushOnButton()
    {
        Push(true);
    }

    private void PushOffButton()
    {
        pushOnTime = TimeSpan.MaxValue; 
        pushOffTime = TimeSpan.MaxValue;
        Push(false);
    }
    
    public void MediaEnded(object? sender, EventArgs e)
    {
        if (!isEnabled)
        {
            return;
        }

        if (isPlaying)
        {
            PushOffButton();
        }
    }

    private bool isOpened(IEnumerable<SignalInfo> signals)
    {
        return signals.Any(signal => signal.phase != "R");
    }

    private static Task<double> GetMelodyLength(Uri uri)
    {
        var player = new MediaPlayer();
        var tcs = new TaskCompletionSource<double>();
        EventHandler? handler = null;
        handler = (sender, args) =>
        {
            player.MediaOpened -= handler;
            tcs.SetResult(player.NaturalDuration.TimeSpan.TotalSeconds);
        };
        player.MediaOpened += handler;
        player.Open(uri);
        return tcs.Task;
    }
}