namespace Helper
{
    using Microsoft.Extensions.Logging;
    using Serilog;
    using Serilog.Core;
    using Serilog.Events;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public static class Logger
    {
        public static readonly ILogger Writer = CreateLogger("SqlReaderModule");

        private static ILogger CreateLogger(string categoryName, LogEventLevel logEventLevel = LogEventLevel.Debug)
        {
            var levelSwitch = new LoggingLevelSwitch(logEventLevel);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            return new LoggerFactory().AddSerilog().CreateLogger(categoryName);
        }
    }

}