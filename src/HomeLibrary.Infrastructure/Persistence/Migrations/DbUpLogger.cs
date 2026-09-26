using System.Globalization;
using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;

namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Forwards DbUp messages to <see cref="ILogger"/>. DbUp passes composite format strings, so they are formatted first.
/// </summary>
/// <param name="logger">Target logger.</param>
internal sealed class DbUpLogger(ILogger logger) : IUpgradeLog
{
    public void LogTrace(string format, params object[] args) => Write(LogLevel.Trace, null, format, args);

    public void LogDebug(string format, params object[] args) => Write(LogLevel.Debug, null, format, args);

    public void LogInformation(string format, params object[] args) => Write(LogLevel.Information, null, format, args);

    public void LogWarning(string format, params object[] args) => Write(LogLevel.Warning, null, format, args);

    public void LogError(string format, params object[] args) => Write(LogLevel.Error, null, format, args);

    public void LogError(Exception ex, string format, params object[] args) => Write(LogLevel.Error, ex, format, args);

    private void Write(LogLevel level, Exception? exception, string format, object[] args)
    {
        if (!logger.IsEnabled(level))
        {
            return;
        }

        var message = args.Length > 0 ? string.Format(CultureInfo.InvariantCulture, format, args) : format;

        logger.Log(level, exception, "DbUp: {Message}", message);
    }
}
