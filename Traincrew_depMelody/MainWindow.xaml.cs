using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrainCrew;

namespace Traincrew_depMelody
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isPlaying = false;
        private int trackNumber = 0;
        private readonly MediaPlayer player;
        private TrainState previousState;
        private GameScreen previousGameScreen = GameScreen.NotRunningGame;
        public MainWindow()
        {
            InitializeComponent();
            player = new MediaPlayer();
            TrainCrewInput.Init();
            previousState = TrainCrewInput.GetTrainState();

            CompositionTarget.Rendering += Update; 
            player.MediaEnded += AudioLoop;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 一応の型チェック
            if(sender is not Button)
            {
                return;
            }
            Button button = (Button)sender;
            bool willPlaying = button.Name == "ON";
            // スイッチの状態が変わらない場合は何もしない
            if(isPlaying == willPlaying)
            {
                return;
            }
            isPlaying = willPlaying;
            // 何か鳴っているなら止める
            player.Stop();
            // Traincrewに乗降促進を押したor解除したと認識させる
            TrainCrewInput.SetButton(InputAction.JoukouSokusin, willPlaying);
            if(trackNumber <= 0)
            {
                return;
            }
            if (willPlaying)
            { 
                // Todo: どの発車メロディを鳴らすかを決める
                // 発車メロディを再生
                var path = Environment.CurrentDirectory + @"\sound\" + previousState.nextStaName + "_" + trackNumber + ".wav";
                // ファイルが存在しない場合はデフォルトの発車メロディを鳴らす
                if (System.IO.File.Exists(path))
                {
                    player.Open(new Uri(path));
                }
                else
                {
                    player.Open(new Uri(Environment.CurrentDirectory + @"\sound\default.wav"));
                }
                player.Play();
            }
            else
            {
                // ドアが閉まりますを鳴らす
                player.Open(new Uri(Environment.CurrentDirectory + @"\sound\doorClosing_" + trackNumber + ".wav"));
                player.Play();
            }
        }

        private void AudioLoop(object? sender, EventArgs e)
        {
            if (!isPlaying)
            {
                return;
            }
            player.Position = TimeSpan.Zero;
            player.Play();
        }


        private void Update(object? sender, EventArgs args)
        {
            TrainState state = TrainCrewInput.GetTrainState();
            GameScreen currentGameScreen = TrainCrewInput.gameState.gameScreen;
            Update2(state, currentGameScreen);
            previousState = state;
            previousGameScreen = currentGameScreen;
        }

        private void Update2(TrainState state, GameScreen currentGameScreen)
        {
            // プレイ中のみ処理
            if (currentGameScreen == GameScreen.MainGame)
            {
                // 何番線が判断する
                trackNumber = GetTrackNumber(state);
                // 停車中ならボタンを有効化、走行中なら無効化
                ChangeButtonIsEnabled(trackNumber > 0);
                // 走行中に発車メロディが鳴りっぱなしなら強制的に止める(ドアが閉まりますは止めない)
                if (trackNumber <= 0 && isPlaying)
                {
                    player.Stop();
                }
            }
            // それ以外の画面ではボタンを無効化
            else
            {
                ChangeButtonIsEnabled(false);
            }
            // ゲーム画面が変わってない場合
            if (previousGameScreen == currentGameScreen)
            {
                return;
            }
            // ポーズした場合の処理
            if (currentGameScreen == GameScreen.MainGame_Pause)
            {
                // PlayerをPause
                player.Pause();
                return;
            }
            // ポーズから再開した場合の処理
            if (previousGameScreen == GameScreen.MainGame_Pause && currentGameScreen == GameScreen.MainGame)
            {
                // PlayerをPause中ならPlayを再開
                player.Play();
                return;
            }
            // プレイ中からそれ以外に変化した場合の処理
            if (previousGameScreen == GameScreen.MainGame && currentGameScreen != GameScreen.MainGame)
            {
                // PlayerをStop
                player.Stop();
                return;
            }
            // ロード中からプレイ中に変化した場合の処理
            if (previousGameScreen == GameScreen.MainGame_Loading && currentGameScreen == GameScreen.MainGame)
            {
                TrainCrewInput.RequestStaData();
                return;
            }
        }

        private void ChangeButtonIsEnabled(bool isEnabled)
        {
            ON.IsEnabled = isEnabled;
            OFF.IsEnabled = isEnabled;
        }

        private static int GetTrackNumber(TrainState state)
        {
            if(state.stationList.Count <= state.nowStaIndex)
            {
                return -1;
            }
            // まずは現在の停車駅を取得
            Station station = state.stationList[state.nowStaIndex];
            // もし停車中でない or 駅でなければ-1を返す
            if(
                float.Abs(station.TotalLength - state.TotalLength) > 3.0f
                || state.Speed != 0.0f
                || station.Name.Contains("信号場", StringComparison.CurrentCulture)
                || station.Name.Contains("引上線", StringComparison.CurrentCulture)
            )
            {
                return -1;
            }
            int number = GetFirstNumber(station.StopPosName);
            // 館浜は、特急とそれ以外で少し処理を変える
            if (station.Name == "館浜")
            {
                // 特急なら、停車位置通りの番号を返す
                if (state.Class == "特急")
                {
                    return number;
                }
                // それ以外であれば、停車位置+1を返す
                return number + 1;
            }
            // 停車位置が取れたらそれを返す
            if (number > 0)
            {
                return number;
            }
            // 停車位置から判断できない場合は、上りか下りかで判断する(上りなら1, 下りなら2)
            int x = state.diaName.Last(char.IsDigit) - '0';
            if (x % 2 == 0)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }
        
        private static int GetFirstNumber(string name)
        {
            // nameの中から一番最初の数字を取り出す
            try
            {
                char c = name.First(char.IsDigit);
                return c - '0';
            }
            catch
            {
                return -1;
            }
        }
    }
}