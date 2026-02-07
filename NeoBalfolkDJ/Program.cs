using Avalonia;
using System;
using System.Threading.Tasks;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ;

internal static class Program
{
    /// <summary>
    /// Early-initialized logging service instance, available before DI container is built.
    /// Used by global exception handlers and passed to DI container.
    /// </summary>
    public static ILoggingService Logger { get; private set; } = null!;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize logging service FIRST, before anything else
        Logger = new LoggingService();
        
        // Set up global exception handlers immediately
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            Logger.Info("=== NeoBalfolkDJ Starting ===");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Logger.Critical("Fatal exception during startup", ex);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception ?? new Exception($"Unknown exception: {e.ExceptionObject}");
        Logger.Critical($"Unhandled exception (IsTerminating: {e.IsTerminating})", ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Mark as observed to prevent app crash
        e.SetObserved();
        Logger.Critical("Unobserved task exception", e.Exception);
    }
}