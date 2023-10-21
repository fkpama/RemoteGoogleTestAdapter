using Microsoft.Extensions.Logging;

namespace GTestAdapter.Core
{
    internal static partial class LoggerMessages
    {
        [LoggerMessage(EventId = 1,
            Level = LogLevel.Information,
            Message = "Starting test discovery: {sourcePath}")]
        public static partial void StartingDiscovery(this ILogger<TestDiscoveryContext> log, string sourcePath);
    }
}
