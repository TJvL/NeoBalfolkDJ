using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

public interface ILoggingService
{
    void Log(LoggingLevel level, string message);
}
