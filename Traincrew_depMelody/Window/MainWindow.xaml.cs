using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Enum;
using Traincrew_depMelody.Repository;
using Traincrew_depMelody.Service;

namespace Traincrew_depMelody.Window;

public interface IMainWindow
{
    void SetButtonIsEnabled(bool enabled);
    ButtonState GetButtonState();
    IAudioPlayerRepository GetAudioPlayerRepository();
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : IMainWindow
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _task;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IAudioPlayerRepository _audioPlayerRepository;
    private ButtonState _buttonState = ButtonState.NotChanged;

    public MainWindow(IServiceScopeFactory serviceScopeFactory)
    {
        _audioPlayerRepository = new AudioPlayerRepository(new(), new(), Dispatcher);
        InitializeComponent();
        _serviceScopeFactory = serviceScopeFactory;
        _task = Task.Run(async () => await RunAsync(_cancellationTokenSource.Token), CancellationToken.None);
        Closing += (_, _) => { OnClosed(); };
    }
    
    private void OnClosed()
    {
        _cancellationTokenSource.Cancel();
        _task.GetAwaiter().GetResult();
    }
    
    private void ButtonPressed(object sender, RoutedEventArgs e)
    {
        // 一応の型チェック
        if (sender is not Button button)
        {
            return;
        }

        _buttonState = button.Name == "ON" ? ButtonState.On : ButtonState.Off;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
            var timer = Task.Delay(TimeSpan.FromMilliseconds(16), cancellationToken);

            try
            {
                var service = scope.ServiceProvider.GetRequiredService<MainService>();
                await service.Tick();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while executing the task.");
            }
            _buttonState = ButtonState.NotChanged;

            try
            {
                await timer;
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    public void SetButtonIsEnabled(bool enabled)
    {
        Dispatcher.Invoke(() =>
        {
            ON.IsEnabled = enabled;
            OFF.IsEnabled = enabled;
        });
    }

    public ButtonState GetButtonState()
    {
        return _buttonState;
    }

    public IAudioPlayerRepository GetAudioPlayerRepository()
    {
        return _audioPlayerRepository;
    }

    public Dispatcher GetDispatcher()
    {
        return Dispatcher;
    }
}