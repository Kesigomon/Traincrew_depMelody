using System.Windows;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
        services.AddSingleton<AppConfiguration>();

        // Presentation Layer
        services.AddSingleton<MainWindow>();

        // Application Layer
        services.AddSingleton<IMelodyControlService, MelodyControlService>();
        services.AddSingleton<IAutoModeService, AutoModeService>();
        services.AddSingleton<IAudioPlaybackService, AudioPlaybackService>();

        // Infrastructure Layer - Repositories
        services.AddSingleton<ITrackRepository, TrackRepository>();
        services.AddSingleton<IAudioProfileRepository, AudioProfileRepository>();

        // Infrastructure Layer - External Services
        services.AddSingleton<ITraincrewGameService, TraincrewGameService>();
        services.AddSingleton<IFFmpegService, FFmpegService>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}