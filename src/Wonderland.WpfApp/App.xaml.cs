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
            // Shutdown을 호출하여 정상 종료 (Environment.Exit는 리소스 정리 안됨)
            // Call Shutdown for proper exit (Environment.Exit skips cleanup)
            Shutdown();
            _host = null!;
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
        // Mutex가 이미 존재하는 경우 (중복 실행) 건너뛰기
        // Skip if mutex already exists (duplicate execution)
        if (_host is null)
        {
            base.OnStartup(e);
            return;
        }

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            // TrayIcon을 먼저 명시적으로 Dispose (잔상 방지)
            // Explicitly dispose TrayIcon first (prevent ghost icon)
            var trayIcon = _host.Services.GetService<TrayIconService>();
            trayIcon?.Dispose();

            await _host.StopAsync();
            _host.Dispose();
        }

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
