using Traincrew_depMelody.Enum;
using Traincrew_depMelody.Repository;
using Traincrew_depMelody.Window;
using TrainCrew;

namespace Traincrew_depMelody.Service;

public class MainService(
    IMainWindow mainWindow,
    AutoModeService autoModeService,
    IAudioPlayerRepository audioPlayerRepository,
    ITraincrewRepository traincrewRepository,
    ITrackRepository trackRepository,
    MelodyPathService melodyPathService
)
{
    private GameScreen _previousGameScreen = GameScreen.NotRunningGame;
    private bool _isMelodyPlaying;
    private bool _isAutoMode = false;

    public async Task Tick()
    {
        await traincrewRepository.Fetch();
        var gameScreen = traincrewRepository.GetGameScreen();
        if (!IsGamePlaying(_previousGameScreen) && IsGamePlaying(gameScreen))
        {
            OnStartGame();
        }

        if (IsGamePlaying(_previousGameScreen) && !IsGamePlaying(gameScreen))
        {
            OnStopGame();
        }

        if (_previousGameScreen != GameScreen.MainGame_Pause && gameScreen == GameScreen.MainGame_Pause)
        {
            OnPaused();
        }

        if (_previousGameScreen == GameScreen.MainGame_Pause && gameScreen == GameScreen.MainGame)
        {
            OnResumed();
        }

        if (gameScreen == GameScreen.MainGame)
        {
            await TickOnPlaying();
        }


        _previousGameScreen = gameScreen;
    }

    private static bool IsGamePlaying(GameScreen gameScreen)
    {
        return gameScreen is GameScreen.MainGame or GameScreen.MainGame_Pause;
    }

    /// <summary>
    /// 乗務を開始した際の処理
    /// </summary>
    private void OnStartGame()
    {
        _isMelodyPlaying = false;
        autoModeService.Reset();
        audioPlayerRepository.Reset();
        var crewType = traincrewRepository.GetCrewType();
        // Todo: もうちょい細かい制御ができるようにしたい
        _isAutoMode = crewType == CrewType.Driver;
    }

    /// <summary>
    /// 乗務を終了した際の処理
    /// </summary>
    private void OnStopGame()
    {
        audioPlayerRepository.Reset();
    }

    /// <summary>
    /// 乗務中(Pause以外)の場合の毎事処理
    /// </summary>
    private async Task TickOnPlaying()
    {
        // 駅と番線取得
        var trackCircuits = traincrewRepository.GetTrackCircuitSet();
        var trackInfo = trackRepository.GetTrackByTrackCircuits(trackCircuits);

        // ホームトラック上にいればボタンを有効にする
        // あとで: 自動モード無効時の場合のみという条件を追加
        var isOnPlatformTrack = trackInfo != null;
        mainWindow.SetButtonIsEnabled(isOnPlatformTrack && !_isAutoMode);
        if (!isOnPlatformTrack)
        {
            return;
        }

        // まず、On用の処理をするべきか、Off用の処理をするべきか、そのままか判定する 
        var buttonState = await GetButtonState();
        // Buttonを変化させてない場合
        // または、オンに変化しているが、メロディ再生中の場合
        // または、オフに変化しているが、メロディ停止中の場合
        if (
            buttonState == ButtonState.NotChanged
            || (buttonState == ButtonState.On && _isMelodyPlaying)
            || (buttonState == ButtonState.Off && !_isMelodyPlaying)
        )
        {
            return;
        }

        _isMelodyPlaying = buttonState == ButtonState.On;

        // 流すべき音源を決定する
        var audioPath = melodyPathService.GetAudioPath(trackInfo.Value, _isMelodyPlaying);

        // 音声を流す
        if (_isMelodyPlaying)
        {
            audioPlayerRepository.PlayOn(audioPath);
        }
        else
        {
            audioPlayerRepository.PlayOff(audioPath);
        }
    }

    /// <summary>
    /// ON押下時の処理をするべきか、OFF押下時の処理をするべきか、そのままか判定する
    /// </summary>
    /// <returns></returns>
    private async Task<ButtonState> GetButtonState()
    {
        if (_isAutoMode)
        {
            return await autoModeService.GetButtonState();
        }
        return mainWindow.GetButtonState();
    }

    /// <summary>
    /// Pauseした際に起きる処理をまとめる
    /// </summary>
    private void OnPaused()
    {
        mainWindow.SetButtonIsEnabled(false);
        audioPlayerRepository.Pause();
    }

    /// <summary>
    /// Pauseから復帰した際に起きる処理をまとめる
    /// </summary>
    private void OnResumed()
    {
        audioPlayerRepository.Resume();
    }
}