using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Views;
using Serilog;

namespace NTNP.Pricing.Desktop;

/// <summary>
/// Section 33/34 — the desktop client's composition root. Builds the DI container, shows the Login
/// window first; a successful login hands off to the Shell window (Section 23's application shell).
/// No <c>StartupUri</c> in App.xaml — the login/shell handoff is imperative because the shell must
/// not exist (and must not let any screen call the API) until a token is present.
/// </summary>
public partial class App : Application
{
    private IHost _host = null!;

    /// <summary>
    /// Escape hatch for the rare case a view model needs to resolve another view model at runtime
    /// with state the DI container can't supply on its own (e.g. <see cref="ViewModels.ShellViewModel"/>
    /// pre-setting a filter on a freshly-resolved <see cref="ViewModels.ProjectListViewModel"/> before
    /// navigating to it). Every other dependency is normal constructor injection.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // We manage window lifetime manually (Login → Shell → back to Login on logout), so the
        // default OnLastWindowClose would tear the whole process down the instant the Login window
        // closes on a *successful* sign-in (there is briefly no window open between closing Login
        // and showing the Shell). Shutdown is always called explicitly instead.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NTNP", "Pricing", "Logs");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(Path.Combine(logDirectory, "ntnp-pricing-desktop-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31)
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled AppDomain exception");
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled dispatcher exception");
            MessageBox.Show(
                "خطای غیرمنتظره‌ای رخ داد. جزئیات در فایل گزارش برنامه ثبت شد." + Environment.NewLine + args.Exception.Message,
                "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true; // keep the app alive — Section 23: an unhandled error must never silently kill the whole session
        };

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) => services.AddNtnpDesktop())
            .Build();
        Services = _host.Services;

        base.OnStartup(e);

        ShowLoginWindow();
    }

    private void ShowLoginWindow()
    {
        var login = _host.Services.GetRequiredService<LoginWindow>();
        login.Closed += (_, _) =>
        {
            // The login window's own code-behind decides whether it closed because of a successful
            // sign-in (session.IsAuthenticated is true — see LoginViewModel) or the user quit.
            var session = _host.Services.GetRequiredService<AppSession>();
            if (session.IsAuthenticated)
                ShowShell();
            else
                Shutdown();
        };
        login.Show();
    }

    private void ShowShell()
    {
        var shell = _host.Services.GetRequiredService<ShellWindow>();
        shell.Closed += (_, _) =>
        {
            if (shell.IsLoggingOut)
                ShowLoginWindow();
            else
                Shutdown();
        };
        shell.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        _host.Dispose();
        base.OnExit(e);
    }
}
