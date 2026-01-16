using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wonderland.Application.Interfaces;
using Wonderland.Application.Services;
using Wonderland.Application.Settings;
using Wonderland.Infrastructure.Persistence;
using Wonderland.Infrastructure.Settings;
using Wonderland.ViewModels;
using Wonderland.WpfServices;

namespace Wonderland.WpfApp;

/// <summary>
/// 앱 진입점 - GenericHost 기반 DI 설정
/// App entry point - GenericHost-based DI configuration
/// </summary>
public partial class App : System.Windows.Application
{
    private const string MutexName = "Wonderland_SingleInstance_Mutex";
    private static Mutex? _mutex;
    private readonly IHost _host;

    public App()
    {
        // 중복 실행 방지
        // Prevent duplicate execution
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("Wonderland is already running.", "Wonderland", MessageBoxButton.OK, MessageBoxImage.Information);
            Environment.Exit(0);
            return;
        }

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    /// <summary>
    /// 서비스 등록
    /// Register services
    /// </summary>
    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Settings
        services.AddSingleton<ISettingsService, JsonSettingsService>();

        // Application Services
        services.AddSingleton<ParallaxCalculator>();
        services.AddSingleton<SceneService>();

        // Infrastructure
        services.AddSingleton<ISceneRepository, JsonSceneRepository>();

        // WPF Services
        services.AddSingleton<MouseTrackingService>();
        services.AddSingleton<WindowModeService>();
        services.AddSingleton<GlobalMouseHook>();
        services.AddSingleton<GlobalKeyboardHook>();
        services.AddSingleton<TrayIconService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        // Mutex 해제
        // Release mutex
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();

        base.OnExit(e);
    }

    /// <summary>
    /// 서비스 제공자
    /// Service provider
    /// </summary>
    public static IServiceProvider Services => ((App)Current)._host.Services;
}
