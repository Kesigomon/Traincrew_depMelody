using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Traincrew_depMelody.Application.Services;
using Traincrew_depMelody.Domain.Interfaces;
using Traincrew_depMelody.Domain.Interfaces.Repositories;
using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;
using Traincrew_depMelody.Infrastructure.ExternalServices;
using Traincrew_depMelody.Infrastructure.Repositories;
using Traincrew_depMelody.Presentation.Views;

namespace Traincrew_depMelody;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    private const int AttachParentProcess = -1;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

#if DEBUG
        // デバッグ時：親プロセス（Visual Studio）のコンソールにアタッチ
        AttachConsole(AttachParentProcess);
        Console.OutputEncoding = Encoding.UTF8;
#endif

        // 作業ディレクトリを実行ファイルの場所に統一する
        // ショートカット起動やパブリッシュ後でも appsettings.json / stations.csv / profiles/ の相対パスが安定する
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Debug);
        });

        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var appConfig = configuration.Get<AppConfiguration>() ?? new AppConfiguration();
        services.AddSingleton(appConfig);

        var autoModeConfig = configuration.GetSection("AutoModeConfig").Get<AutoModeConfig>() ?? new AutoModeConfig();
        services.AddSingleton(autoModeConfig);

        var serialButtonConfig = configuration.GetSection("SerialButton").Get<SerialButtonConfig>() ?? new SerialButtonConfig();
        services.AddSingleton(serialButtonConfig);

        // Presentation Layer
        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddSingleton<Func<SettingsWindow>>(sp => sp.GetRequiredService<SettingsWindow>);

        // Application Layer
        services.AddSingleton<IMelodyControlService, MelodyControlService>();
        services.AddSingleton<IAutoModeService, AutoModeService>();
        services.AddSingleton<IAudioPlaybackService, AudioPlaybackService>();

        // Infrastructure Layer - Repositories
        services.AddSingleton<ITrackRepository, TrackRepository>();
        services.AddSingleton<IAudioProfileRepository, AudioProfileRepository>();
        services.AddSingleton<IConfigurationPersistenceService, ConfigurationPersistenceService>();

        // Infrastructure Layer - External Services
        services.AddSingleton<ITraincrewGameService, TraincrewGameService>();
        services.AddSingleton<IFFmpegService, FFmpegService>();
        services.AddSingleton<ISerialButtonService, SerialButtonService>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}