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
/// </summary>
public partial class App : System.Windows.Application
{
    private const string MutexName = "Wonderland_SingleInstance_Mutex";
    private static Mutex? _mutex;
    private readonly IHost _host;

    public App()
    {
        // 중복 실행 방지
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("Wonderland is already running.", "Wonderland", MessageBoxButton.OK, MessageBoxImage.Information);
            // Shutdown을 호출하여 정상 종료 (Environment.Exit는 리소스 정리 안됨)
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
    /// </summary>
    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, JsonSettingsService>();

        services.AddSingleton<ParallaxCalculator>();
        services.AddSingleton<SceneService>();

        services.AddSingleton<ISceneRepository, JsonSceneRepository>();

        services.AddSingleton<MouseTrackingService>();
        services.AddSingleton<WindowModeService>();
        services.AddSingleton<GlobalMouseHook>();
        services.AddSingleton<GlobalKeyboardHook>();
        services.AddSingleton<TrayIconService>();

        services.AddSingleton<MainViewModel>();

        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Mutex가 이미 존재하는 경우 (중복 실행) 건너뛰기
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
            var trayIcon = _host.Services.GetService<TrayIconService>();
            trayIcon?.Dispose();

            await _host.StopAsync();
            _host.Dispose();
        }

        // Mutex 해제
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();

        base.OnExit(e);
    }

    /// <summary>
    /// 서비스 제공자
    /// </summary>
    public static IServiceProvider Services => ((App)Current)._host.Services;
}
