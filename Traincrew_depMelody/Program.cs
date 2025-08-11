using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Traincrew_depMelody.Repository;
using Traincrew_depMelody.Service;
using Traincrew_depMelody.Window;

namespace Traincrew_depMelody;

public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var app = new App();
        var host = CreateHostBuilder(args).Build();
        var mainWindow = host.Services.GetService<MainWindow>();
        host.StartAsync().GetAwaiter().GetResult();
        app.Run(mainWindow);
        host.StopAsync().GetAwaiter().GetResult();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IAudioPlayerRepository>(serviceProvider =>
                    serviceProvider.GetRequiredService<IMainWindow>().GetAudioPlayerRepository());
                services.AddSingleton<IFFmpegRepository, FFmpegRepository>();
                // services.AddSingleton<IKeyboardRepository, KeyboardRepository>();
                // services.AddSingleton<ISerialPortRepository, SerialPortRepository>();
                services.AddSingleton<ITrackRepository, TrackRepository>();
                services.AddSingleton<ITraincrewRepository, TraincrewRepository>();

                services.AddSingleton<AutoModeService>();
                services.AddSingleton<MainService>();
                services.AddSingleton<MelodyPathService>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<IMainWindow>(serviceProvider => serviceProvider.GetRequiredService<MainWindow>());
            });
    }
}