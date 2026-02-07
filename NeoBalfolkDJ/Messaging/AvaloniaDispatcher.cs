using System;
using Avalonia.Threading;

namespace NeoBalfolkDJ.Messaging;

/// <summary>
/// Avalonia implementation of IDispatcher using Dispatcher.UIThread.
/// </summary>
public class AvaloniaDispatcher : IDispatcher
{
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Dispatcher.UIThread.Post(action);
    }

    public bool CheckAccess()
    {
        return Dispatcher.UIThread.CheckAccess();
    }
}

