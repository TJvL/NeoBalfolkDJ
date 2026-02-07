using System;
using System.Threading.Tasks;

namespace NeoBalfolkDJ.Messaging;

/// <summary>
/// Command bus for sending commands to handlers.
/// Commands represent requests for actions to be performed.
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Sends a command to its registered handler.
    /// Exceptions are caught, logged, and shown as notifications.
    /// </summary>
    Task SendAsync<TCommand>(TCommand command) where TCommand : class;

    /// <summary>
    /// Registers a handler for commands of type TCommand.
    /// Only one handler per command type is allowed.
    /// </summary>
    /// <returns>A disposable token. Dispose to unregister the handler.</returns>
    IDisposable RegisterHandler<TCommand>(Func<TCommand, Task> handler) where TCommand : class;
}

