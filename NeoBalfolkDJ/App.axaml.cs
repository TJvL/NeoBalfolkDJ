using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;
using NeoBalfolkDJ.ViewModels;
using NeoBalfolkDJ.Views;

namespace NeoBalfolkDJ;

// ReSharper disable once PartialTypeWithSinglePart
public partial class App : Application
{
    /// <summary>
    /// Gets the DI service provider.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Build DI container with pre-created logging service from Program
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddNeoBalfolkDj(Program.Logger);
        Services = serviceCollection.BuildServiceProvider();

        // Apply theme from settings on startup
        ApplyThemeFromSettings();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();


            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };

            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        // Dispose the service provider on shutdown
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void ApplyThemeFromSettings()
    {
        var settingsService = Services.GetRequiredService<ISettingsService>();
        var settings = settingsService.Load();
        ApplyTheme(settings.Theme);
    }

    public void ApplyTheme(AppTheme theme)
    {
        RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default // Auto follows system
        };
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}