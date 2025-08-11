using System.Windows.Media;
using System.Windows.Threading;
using Traincrew_depMelody.Window;

namespace Traincrew_depMelody.Repository;

public interface IAudioPlayerRepository
{
    void PlayOn(Uri path, Action? onComplete = null);
    void PlayOff(Uri path, Action? onComplete = null, TimeSpan? duration = null);
    void Resume();
    void Pause();
    void Reset();
}

public class AudioPlayerRepository : IAudioPlayerRepository
{
    private readonly MediaPlayer _playerOn;
    private readonly MediaPlayer _playerOff;
    private readonly Dispatcher _dispatcher;
    private EventHandler? _onCompleteOn;
    private EventHandler? _onCompleteOff;

    public AudioPlayerRepository(MediaPlayer playerOn, MediaPlayer playerOff, Dispatcher dispatcher)
    {
        _playerOn = playerOn;
        _playerOff = playerOff;
        _dispatcher = dispatcher;
        _playerOn.MediaEnded += (s, e) => _dispatcher.Invoke(() =>
        {
            _playerOn.Position = TimeSpan.Zero;
            _playerOn.Play();
        });
        _playerOff.MediaEnded += (s, e) => _dispatcher.Invoke(() =>
        {
            _playerOff.Stop();
            _playerOff.Close();
        });
    }

    public void PlayOn(Uri path, Action? onComplete = null)
    {
        _dispatcher.Invoke(() =>
        {
            _playerOn.Open(path);
            if (_onCompleteOn != null)
            {
                _playerOn.MediaEnded -= _onCompleteOn;
                _onCompleteOn = null;
            }

            if (onComplete != null)
            {
                _onCompleteOn = (s, e) => onComplete.Invoke();
                _playerOn.MediaEnded += _onCompleteOn;
            }

            _playerOn.Play();
        });
    }

    public void PlayOff(Uri path, Action? onComplete = null, TimeSpan? duration = null)
    {
        // Todo: 1秒待ち時にPauseした時、Pauseを無視してしまう
        Task.Delay(1000).ContinueWith(_ => _dispatcher.Invoke(() =>
        {
            _playerOn.Stop();
            _playerOn.Close();
            _playerOff.Stop();
            if (_onCompleteOff != null)
            {
                _playerOff.MediaEnded -= _onCompleteOff;
                _onCompleteOff = null;
            }

            if (onComplete != null)
            {
                _onCompleteOff = (s, e) => onComplete.Invoke();
                _playerOff.MediaEnded += _onCompleteOff;
            }

            _playerOff.Open(path);
            _playerOff.Play();
        }));
    }

    public void Resume()
    {
        _dispatcher.Invoke(() =>
        {
            _playerOn.Play();
            _playerOff.Play();
        });
    }

    public void Pause()
    {
        _dispatcher.Invoke(() =>
        {
            _playerOn.Pause();
            _playerOff.Pause();
        });
    }

    public void Reset()
    {
        _dispatcher.Invoke(() =>
        {
            _playerOn.Stop();
            _playerOn.Close();
            _playerOff.Stop();
            _playerOff.Close();
        });
    }
}