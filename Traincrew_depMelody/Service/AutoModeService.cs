using System.Diagnostics;
using Traincrew_depMelody.Enum;
using Traincrew_depMelody.Models;
using Traincrew_depMelody.Repository;
using Traincrew_depMelody.Window;

namespace Traincrew_depMelody.Service;

public class AutoModeService
{
    private readonly IFFmpegRepository _ffmpegRepository;
    private readonly ITraincrewRepository _traincrewRepository;
    private readonly ITrackRepository _trackRepository;
    private readonly MelodyPathService _melodyPathService;
    private const double MinimumDoorOpenTime = 12.0;
    private double _melodyDurationMargin = 8.5;
    private bool _isPlaying;
    private double _arrivingTimeSeconds;
    private double _signalOpenedTimeSeconds;
    private double _melodyDuration;
    private double _doorClosingDuration;
    private int _previousTrackNumber;
    private bool _previousAllClose;
    private bool _previousIsSignalOpened;

    public AutoModeService(IFFmpegRepository ffmpegRepository, ITraincrewRepository traincrewRepository,
        ITrackRepository trackRepository, MelodyPathService melodyPathService)
    {
        _traincrewRepository = traincrewRepository;
        _trackRepository = trackRepository;
        _melodyPathService = melodyPathService;
        _ffmpegRepository = ffmpegRepository;
        Reset();
    }

    public void Reset()
    {
        _isPlaying = false;
        _previousTrackNumber = 0;
        _previousAllClose = true;
        _previousIsSignalOpened = false;
        _melodyDuration = 0.0;
        _doorClosingDuration = 0.0;
        _arrivingTimeSeconds = 0.0;
        _signalOpenedTimeSeconds = 0.0;
    }

    public async Task<ButtonState> GetButtonState()
    {
        var state = _traincrewRepository.GetTrainState();
        _melodyDurationMargin = state.CarStates.Count > 0 && state.CarStates[0].CarModel == "50000" ? 16.5 : 8.5;

        var trackCircuits = _traincrewRepository.GetTrackCircuitSet();
        var trackInfo = _trackRepository.GetTrackByTrackCircuits(trackCircuits);
        var trackNumber = trackInfo?.Item2 ?? -1;

        var result = await _elapse(state, trackInfo);
        _isPlaying = result switch
        {
            ButtonState.On => true,
            ButtonState.Off => false,
            _ => _isPlaying
        };

        _previousTrackNumber = trackNumber;
        _previousAllClose = state.AllClose;
        _previousIsSignalOpened = IsSignalOpened();
        return result;
    }

    private async Task<ButtonState> _elapse(AppTrainState state, (string, int)? trackInfo)
    {
        var nowTime = state.NowTime;
        var nowTimeSeconds = nowTime.TotalSeconds;
        // ドアを開けたら、到着時刻を記録する
        if (_previousAllClose && !state.AllClose)
        {
            _arrivingTimeSeconds = nowTimeSeconds;
        }

        // ドアが開いている状態で、信号が開通した時刻を記録する
        if (!state.AllClose && !_previousIsSignalOpened && IsSignalOpened())
        {
            _signalOpenedTimeSeconds = nowTimeSeconds;
        }

        // ホームトラックにいない場合、早期Return
        if (trackInfo == null)
        {
            return ButtonState.Off;
        }

        // ホームトラックに在線したら、とりあえずメロディーの長さをセットする
        if (_previousTrackNumber <= 0)
        {
            await SetMelodyDuration(trackInfo.Value);
        }

        // 閉扉しているか、信号が開いていない場合、とりあえず止める
        if (state.AllClose || !IsSignalOpened())
        {
            return ButtonState.Off;
        }

        var departureTime = state.StationList[state.NowStaIndex].DepTime.TotalSeconds;
        var pushOnSeconds = new[]
        {
            // 電車到着後2秒後
            _arrivingTimeSeconds + 2.0,
            // 信号開通1秒後
            _signalOpenedTimeSeconds + 1.0,
            // 発車時刻 - メロディ時間 - ドア閉まる時間 - マージン
            departureTime - _melodyDuration - _doorClosingDuration - _melodyDurationMargin
        }.Max();
        var pushOffSeconds = new[]
        {
            // ONを押してから最低でも1秒後
            pushOnSeconds + 1.0,
            // ドアが開いてから最低でも15秒後
            _arrivingTimeSeconds + MinimumDoorOpenTime - _doorClosingDuration,
            // 発車時刻 - ドア閉まる時間 - マージン
            departureTime - _doorClosingDuration - _melodyDurationMargin,
            // ONを押した時刻 + メロディの長さ + マージン(つまり、必ず全部鳴らし切る)
            // pushOnSeconds + melodyDuration + 1.0,
        }.Max();
        Debug.WriteLine($"{pushOnSeconds}, {pushOffSeconds}");
        if (pushOnSeconds <= nowTimeSeconds && nowTimeSeconds < pushOffSeconds)
        {
            return ButtonState.On;
        }
        return ButtonState.Off;
    }

    private async Task SetMelodyDuration((string, int) trackInfo)
    {
        _melodyDuration = await _ffmpegRepository
            .GetDuration(_melodyPathService.GetAudioPath(trackInfo, true));
        _doorClosingDuration = await _ffmpegRepository
            .GetDuration(_melodyPathService.GetAudioPath(trackInfo, false));
    }

    public void MediaEnded(object? sender, EventArgs e)
    {
        if (_isPlaying)
        {
            // Todo: Offを押す
        }
    }

    private bool IsSignalOpened()
    {
        var signals = _traincrewRepository.GetSignalInfos();
        return signals.Any(signal => !signal.name.StartsWith("入換") && signal.phase != "R");
    }
}