using Microsoft.Extensions.DependencyInjection;
using NeoBalfolkDJ.Messaging;
using NeoBalfolkDJ.Services;
using NeoBalfolkDJ.ViewModels;

namespace NeoBalfolkDJ;

/// <summary>
/// Extension methods for configuring dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all NeoBalfolkDJ services, handlers, and ViewModels to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="loggingService">Pre-created logging service instance (created early for global exception handlers).</param>
    public static IServiceCollection AddNeoBalfolkDj(this IServiceCollection services, ILoggingService loggingService)
    {
        // Infrastructure - use pre-created logging service
        services.AddSingleton<IDispatcher, AvaloniaDispatcher>();
        services.AddSingleton(loggingService);
        services.AddSingleton<IEventAggregator, EventAggregator>();
        services.AddSingleton<ICommandBus, CommandBus>();

        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IMusicScannerService, MusicScannerService>();
        services.AddSingleton<ITrackStoreService, TrackStoreService>();
        services.AddSingleton<ISessionTrackHistoryService, SessionTrackHistoryService>();
        services.AddSingleton<ITrackPreloadService, TrackPreloadService>();
        services.AddSingleton<IDanceTreeHistoryService, DanceTreeHistoryService>();
        services.AddSingleton<IPlaybackService, NetCoreAudioPlaybackService>();
        services.AddSingleton<IDanceCategoryService, DanceCategoryService>();
        services.AddSingleton<IDanceSynonymService, DanceSynonymService>();
        services.AddSingleton<IWeightedRandomService, WeightedRandomService>();
        // Note: PresentationDisplayService requires Window owner, registered separately

        // ViewModels
        services.AddSingleton<TrackListViewModel>();
        services.AddSingleton<QueueViewModel>();
        services.AddSingleton<PlaybackViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ToolbarViewModel>();
        services.AddSingleton<NotificationViewModel>();
        services.AddSingleton<HelpViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        return services;
    }
}




