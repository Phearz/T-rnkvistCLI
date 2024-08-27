using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Events;

namespace TörnkvistCLI
{

    public static class LoggingConfig
    {
        public static ILoggerFactory CreateLoggerFactory(string loggingLevel)
        {
            LogEventLevel level = ParseLoggingLevel(loggingLevel);

            //confa Serillog dör att logga till fil 
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.File("logs/application.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            return new SerilogLoggerFactory(Log.Logger);
        }
        private static LogEventLevel ParseLoggingLevel(string loggingLevel)
        {
            return loggingLevel.ToLower() switch
            {
                "debug" => LogEventLevel.Debug,
                "information" => LogEventLevel.Information,
                "warning" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information, // Standardnivå om inget matchar
            };
        }
    }
}