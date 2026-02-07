# Architecture Documentation

This document provides a developer-friendly overview of the NeoBalfolkDJ architecture. It covers the key patterns and concepts used throughout the codebase.

## Table of Contents

- [MVVM Pattern](#mvvm-pattern)
- [Dependency Injection & Services](#dependency-injection--services)
- [Event Aggregator](#event-aggregator)
- [Command Bus](#command-bus)
- [Logging](#logging)
- [Notifications](#notifications)

---

## MVVM Pattern

NeoBalfolkDJ uses the **Model-View-ViewModel (MVVM)** pattern, which separates UI concerns from business logic.

### How It Works

```mermaid
flowchart TB
    subgraph View ["View (XAML + Code-behind)"]
        V1[MainWindow.axaml]
        V2[QueueView.axaml]
        V3[PlaybackView.axaml]
    end
    
    subgraph ViewModel ["ViewModel (C# Classes)"]
        VM1[MainWindowViewModel]
        VM2[QueueViewModel]
        VM3[PlaybackViewModel]
    end
    
    subgraph Model ["Model (Data Classes)"]
        M1[Track]
        M2[ApplicationSettings]
        M3[DanceCategory]
    end
    
    V1 -->|DataBinding| VM1
    V2 -->|DataBinding| VM2
    V3 -->|DataBinding| VM3
    
    VM1 --> M1
    VM1 --> M2
    VM2 --> M1
    VM3 --> M1
```

| Layer | Responsibility | Examples |
|-------|---------------|----------|
| **View** | UI rendering, user input | `MainWindow.axaml`, `QueueView.axaml` |
| **ViewModel** | UI logic, state management, commands | `MainWindowViewModel`, `QueueViewModel` |
| **Model** | Data structures, business entities | `Track`, `ApplicationSettings` |

### How to Add New Code

**Adding a new View:**

1. Create `Views/MyNewView.axaml` and `Views/MyNewView.axaml.cs`
2. Create `ViewModels/MyNewViewModel.cs` extending `ViewModelBase`
3. Register the ViewModel in `ServiceCollectionExtensions.cs`:
   ```csharp
   services.AddSingleton<MyNewViewModel>();
   ```
4. Set the `DataContext` in the View or use the `ViewLocator`

**Adding a new property to a ViewModel:**

```csharp
public partial class MyViewModel : ViewModelBase
{
    [ObservableProperty]  // CommunityToolkit.Mvvm generates PropertyChanged
    private string _myProperty = string.Empty;
    
    [RelayCommand]  // Generates ICommand for button binding
    private void DoSomething()
    {
        // Command logic
    }
}
```

---

## Dependency Injection & Services

All services are registered in the DI container and injected via constructors.

### How It Works

```mermaid
flowchart TB
    subgraph DI ["DI Container (IServiceProvider)"]
        direction TB
        REG[ServiceCollectionExtensions.cs]
    end
    
    subgraph Services ["Services (Singletons)"]
        S1[ILoggingService]
        S2[IPlaybackService]
        S3[ITrackStoreService]
        S4[ISettingsService]
        S5[INotificationService]
    end
    
    subgraph ViewModels ["ViewModels"]
        VM1[MainWindowViewModel]
        VM2[PlaybackViewModel]
        VM3[QueueViewModel]
    end
    
    REG --> S1
    REG --> S2
    REG --> S3
    REG --> S4
    REG --> S5
    
    S1 -->|Injected| VM1
    S2 -->|Injected| VM2
    S3 -->|Injected| VM1
    S4 -->|Injected| VM3
    S5 -->|Injected| VM1
```

Services are registered in `ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddNeoBalfolkDj(this IServiceCollection services, ILoggingService loggingService)
{
    // Infrastructure
    services.AddSingleton<IDispatcher, AvaloniaDispatcher>();
    services.AddSingleton(loggingService);
    services.AddSingleton<IEventAggregator, EventAggregator>();
    services.AddSingleton<ICommandBus, CommandBus>();

    // Services
    services.AddSingleton<ISettingsService, SettingsService>();
    services.AddSingleton<INotificationService, NotificationService>();
    services.AddSingleton<ITrackStoreService, TrackStoreService>();
    // ... more services

    // ViewModels
    services.AddSingleton<MainWindowViewModel>();
    services.AddSingleton<QueueViewModel>();
    // ... more ViewModels

    return services;
}
```

### How to Add New Code

**Adding a new service:**

1. Create the interface in `Services/IMyService.cs`:
   ```csharp
   public interface IMyService
   {
       void DoWork();
       Task<string> GetDataAsync();
   }
   ```

2. Create the implementation in `Services/MyService.cs`:
   ```csharp
   public class MyService : IMyService, IDisposable
   {
       private readonly ILoggingService _logger;
       
       public MyService(ILoggingService logger)
       {
           _logger = logger;
       }
       
       public void DoWork() { /* ... */ }
       public Task<string> GetDataAsync() { /* ... */ }
       
       public void Dispose() { /* cleanup */ }
   }
   ```

3. Register in `ServiceCollectionExtensions.cs`:
   ```csharp
   services.AddSingleton<IMyService, MyService>();
   ```

4. Inject into ViewModels or other services:
   ```csharp
   public MyViewModel(IMyService myService)
   {
       _myService = myService;
   }
   ```

---

## Event Aggregator

The Event Aggregator enables **decoupled communication** between ViewModels. Publishers don't know about subscribers.

### How It Works

```mermaid
flowchart LR
    subgraph Publishers
        P1[PlaybackViewModel]
        P2[QueueViewModel]
    end
    
    subgraph EA ["Event Aggregator"]
        direction TB
        E1[TrackStartedEvent]
        E2[TrackFinishedEvent]
        E3[QueueFirstItemChangedEvent]
        E4[HistoryModeChangedEvent]
    end
    
    subgraph Subscribers
        S1[MainWindowViewModel]
        S2[ToolbarViewModel]
        S3[PresentationService]
    end
    
    P1 -->|Publish| E1
    P1 -->|Publish| E2
    P2 -->|Publish| E3
    P2 -->|Publish| E4
    
    E1 -->|Subscribe| S1
    E1 -->|Subscribe| S3
    E2 -->|Subscribe| S1
    E3 -->|Subscribe| S1
    E4 -->|Subscribe| S2
```

### Event Aggregator vs Traditional C# Events

| Aspect | Event Aggregator | Traditional `+=` Events |
|--------|-----------------|------------------------|
| **Coupling** | Loose - no direct reference | Tight - publisher knows subscriber type |
| **Scope** | Application-wide broadcast | Between two known objects |
| **Use case** | Cross-cutting, multiple subscribers | Parent-child, View-ViewModel, 1-to-1 |
| **Memory** | Returns `IDisposable` | Must manually unsubscribe (`-=`) |
| **Threading** | Can marshal to UI thread | Caller's thread |

**Use Event Aggregator when:**
- Multiple unrelated ViewModels need to react to the same event
- Publisher shouldn't know about subscribers
- Event represents a "fact" that happened (past tense naming)

**Use traditional `+=` events when:**
- View needs to show a dialog (file picker, confirmation)
- Direct parent-child relationship
- High-frequency events (e.g., `PropertyChanged`, `CollectionChanged`)
- Type-safe parameters (settings changes with specific types)
- Service-to-ViewModel notifications (1-to-1 relationship)

### How to Add New Code

**Creating a new event:**

1. Create the event record in `Messaging/Events/`:
   ```csharp
   // Messaging/Events/MyNewEvent.cs
   namespace NeoBalfolkDJ.Messaging.Events;
   
   /// <summary>
   /// Event raised when something important happened.
   /// </summary>
   public sealed record MyNewEvent(string Data, int Count);
   ```

2. Publish from a ViewModel:
   ```csharp
   _eventAggregator.Publish(new MyNewEvent("hello", 42));
   ```

3. Subscribe in another ViewModel (store the subscription for disposal):
   ```csharp
   // In constructor or initialization
   _subscriptions.Add(_eventAggregator.Subscribe<MyNewEvent>(evt =>
   {
       // Handle the event
       Console.WriteLine($"Received: {evt.Data}, {evt.Count}");
   }));
   
   // In Dispose()
   foreach (var sub in _subscriptions) sub.Dispose();
   ```

---

## Command Bus

The Command Bus handles **action requests** between ViewModels. Commands are imperative ("do this").

### How It Works

```mermaid
flowchart LR
    subgraph Senders ["Command Senders"]
        T[ToolbarViewModel]
        TL[TrackListViewModel]
        P[PlaybackViewModel]
    end
    
    subgraph CB ["Command Bus"]
        direction TB
        C1[AddTrackToQueueCommand]
        C2[PlayNextTrackCommand]
        C3[ShowSettingsCommand]
        C4[ClearQueueCommand]
    end
    
    subgraph Handler ["Command Handlers (MainWindowViewModel)"]
        H1["Queue.AddTrack()"]
        H2["PlayNextFromQueue()"]
        H3["ShowSettings()"]
        H4["Queue.ClearQueue()"]
    end
    
    T -->|SendAsync| C3
    T -->|SendAsync| C4
    TL -->|SendAsync| C1
    P -->|SendAsync| C2
    
    C1 --> H1
    C2 --> H2
    C3 --> H3
    C4 --> H4
```

### Commands vs Events

| Aspect | Commands | Events |
|--------|----------|--------|
| **Naming** | Imperative verb (`AddTrackToQueueCommand`) | Past tense (`TrackAddedToQueueEvent`) |
| **Intent** | Request an action | Report something happened |
| **Handlers** | Usually one | Can be many |
| **Direction** | ViewModel → Orchestrator | Publisher → Many subscribers |

### How to Add New Code

**Creating a new command:**

1. Create the command record in `Messaging/Commands/`:
   ```csharp
   // Messaging/Commands/MyNewCommand.cs
   namespace NeoBalfolkDJ.Messaging.Commands;
   
   /// <summary>
   /// Command to do something specific.
   /// </summary>
   public sealed record MyNewCommand(string Parameter);
   ```

2. Register handler in `MainWindowViewModel.RegisterCommandHandlers()`:
   ```csharp
   _subscriptions.Add(_commandBus.RegisterHandler<MyNewCommand>(cmd =>
   {
       // Handle the command
       DoSomething(cmd.Parameter);
       return Task.CompletedTask;
   }));
   ```

3. Send from any ViewModel:
   ```csharp
   _commandBus.SendAsync(new MyNewCommand("value"));
   ```

---

## Logging

Centralized logging via `ILoggingService` with file output and log rotation.

### How It Works

```mermaid
flowchart TB
    subgraph Callers ["Any Class"]
        VM[ViewModels]
        SVC[Services]
    end
    
    subgraph LS ["ILoggingService"]
        direction TB
        L1["Debug()"]
        L2["Info()"]
        L3["Warning()"]
        L4["Error()"]
    end
    
    subgraph Output ["Output"]
        F[app.log file]
        C[Console]
    end
    
    VM --> L1
    VM --> L2
    SVC --> L3
    SVC --> L4
    
    L1 --> F
    L2 --> F
    L3 --> F
    L4 --> F
    L2 --> C
    L3 --> C
    L4 --> C
```

### Log Levels

| Level | Use Case | Example |
|-------|----------|---------|
| `Debug` | Detailed diagnostic info | `"Track dequeued: Artist - Title"` |
| `Info` | Normal operations | `"Application started"` |
| `Warning` | Potential issues | `"Failed to preload track"` |
| `Error` | Errors with exception | `"Playback failed"`, includes stack trace |
| `Critical` | Fatal errors, application cannot continue | `"Failed to initialize audio system"` |

### How to Add New Code

**Using the logging service:**

```csharp
public class MyService
{
    private readonly ILoggingService _logger;
    
    public MyService(ILoggingService logger)
    {
        _logger = logger;
    }
    
    public void DoWork()
    {
        _logger.Debug("Starting work...");
        
        try
        {
            // ... work
            _logger.Info("Work completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Work failed", ex);
        }
    }
}
```

**Log file location:** `~/.config/NeoBalfolkDJ/app.log` (Linux) or `%APPDATA%\NeoBalfolkDJ\app.log` (Windows)

---

## Notifications

User-facing notifications displayed in the UI via `INotificationService`.

### How It Works

```mermaid
flowchart LR
    subgraph Callers ["Services / ViewModels"]
        S1[QueueViewModel]
        S2[WeightedRandomService]
        S3[TrackStoreService]
    end
    
    subgraph NS ["INotificationService"]
        N["ShowNotification(message, severity)"]
    end
    
    subgraph Display ["NotificationViewModel"]
        D1["Information (Blue)"]
        D2["Warning (Orange)"]
        D3["Error (Red)"]
    end
    
    S1 --> N
    S2 --> N
    S3 --> N
    
    N --> D1
    N --> D2
    N --> D3
```

### Notification Severities

| Severity | Color | Use Case |
|----------|-------|----------|
| `Information` | Blue | Success messages, confirmations |
| `Warning` | Orange | Non-critical issues, user should be aware |
| `Error` | Red | Failures, something went wrong |

### How to Add New Code

**Showing a notification:**

```csharp
public class MyViewModel
{
    private readonly INotificationService _notifications;
    
    public MyViewModel(INotificationService notifications)
    {
        _notifications = notifications;
    }
    
    public void DoSomething()
    {
        if (success)
        {
            _notifications.ShowNotification("Operation completed!", NotificationSeverity.Information);
        }
        else
        {
            _notifications.ShowNotification("Something went wrong", NotificationSeverity.Error);
        }
    }
}
```

**In QueueViewModel (common pattern):**

```csharp
if (QueuedItems.Count >= MaxQueueItems)
{
    NotificationService?.ShowNotification(
        $"Queue is full (max {MaxQueueItems} items)", 
        NotificationSeverity.Warning);
    return;
}
```

---

## Summary: When to Use What

| Scenario | Pattern |
|----------|---------|
| ViewModel A triggers action in ViewModel B | **Command Bus** |
| Something happened, multiple places care | **Event Aggregator** |
| View needs to show dialog/picker | **Traditional `+=` event** |
| Setting value changed (type-safe) | **Traditional `+=` event** |
| Service notifies ViewModel | **Traditional `+=` event** |
| Need reusable business logic | **Service (DI)** |
| User needs feedback | **INotificationService** |
| Developer needs debugging info | **ILoggingService** |


