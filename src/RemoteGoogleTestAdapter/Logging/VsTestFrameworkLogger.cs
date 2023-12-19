using GoogleTestAdapter.Remote.Adapter.Utils;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Remote.Adapter.Logging
{
    public class VsTestFrameworkLogger : LoggerBase, IMSLogger
    {
        private readonly IMessageLogger? _logger;
        private readonly Func<bool> _timeStampOutput;
        private readonly Lazy<LogLevel> minLevel;

        public VsTestFrameworkLogger(IMessageLogger? logger,
                                     Func<bool> inDebugMode,
                                     Func<bool> timestampOutput)
            : base(inDebugMode)
        {
            this.minLevel = new(() => inDebugMode() ? LogLevel.Debug : LogLevel.Information);
            _logger = logger;
            _timeStampOutput = timestampOutput;
        }

        class FakeScope : IDisposable
        {
            public void Dispose() { }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new FakeScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public override void Log(Severity severity, string message)
        {
            switch (severity)
            {
                case Severity.Info:
                    LogSafe(TestMessageLevel.Informational, message);
                    break;
                case Severity.Warning:
                    LogSafe(TestMessageLevel.Warning, string.Format(Resources.WarningMessage, message));
                    break;
                case Severity.Error:
                    LogSafe(TestMessageLevel.Error, string.Format(Resources.ErrorMessage, message));
                    break;
                default:
                    throw new Exception(string.Format(Resources.UnknownLiteral, severity));
            }
        }

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            if (logLevel < this.minLevel.Value)
            {
                return;
            }
            var lvl = logLevel switch
            {
                LogLevel.Warning => TestMessageLevel.Warning,
                LogLevel.Error => TestMessageLevel.Error,
                _ => TestMessageLevel.Informational
            };

            var msg = formatter(state, exception);
            this.LogSafe(lvl, msg);
        }

        private void LogSafe(TestMessageLevel level, string message)
        {
            if (_timeStampOutput())
                LocUtils.TimestampMessage(ref message);

            if (string.IsNullOrWhiteSpace(message))
            {
                // Visual Studio 2013 is very picky about empty lines...
                // But it accepts an 'INVISIBLE SEPARATOR' (U+2063)  :-)
                message = "\u2063";
            }

            _logger?.SendMessage(level, message);
            ReportFinalLogEntry(
                new LogEntry
                {
                    Severity = level.GetSeverity(),
                    Message = message
                });
        }

    }
}