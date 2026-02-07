using System;

namespace NeoBalfolkDJ.Messaging;

/// <summary>
/// Abstraction for UI thread dispatching, enabling testability.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Posts an action to be executed on the UI thread.
    /// </summary>
    void Post(Action action);

    /// <summary>
    /// Returns true if the current thread is the UI thread.
    /// </summary>
    bool CheckAccess();
}

