using Microsoft.Extensions.Logging;
using Sodiware.IO;
using GoogleTestAdapter.Remote.Discovery;

namespace GoogleTestAdapter.Remote
{
    internal static partial class LoggerMessages
    {
        [LoggerMessage(EventId = 1,
            Level = LogLevel.Information,
            SkipEnabledCheck = true,
            Message = "Starting test discovery: {sourcePath}")]
        static partial void startingDiscovery(this ILogger<TestDiscoveryContext> log, string sourcePath);

        [LoggerMessage(EventId = 2,
            Level = LogLevel.Information,
            SkipEnabledCheck =true,
            Message = "Test discovery for '{source}' completed, overall duration: {elapsed}")]
        static partial void testDiscoveryCompleted(this ILogger<TestDiscoveryContext> log, string source, TimeSpan elapsed);

        public static void TestDiscoveryStarting(this ILogger<TestDiscoveryContext> logger, string source)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                if (PathUtils.IsInsideDirectory(Environment.CurrentDirectory, source))
                    source = PathUtils.MakeRelative(Environment.CurrentDirectory, source);

                startingDiscovery(logger, source);
            }
        }

        public static void TestDiscoveryCompleted(this ILogger<TestDiscoveryContext> logger, string source, TimeSpan elapsed)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                if (PathUtils.IsInsideDirectory(Environment.CurrentDirectory, source))
                    source = PathUtils.MakeRelative(Environment.CurrentDirectory, source);

                testDiscoveryCompleted(logger, source, elapsed);
            }
        }
    }
}
