using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Remote
{
    public static class LoggerExtensions
    {
        public static void LogWarning(this ILogger logger, string message, params object[] args)
        {
            logger.LogWarning(string.Format(message, args));
        }
        public static void LogError(this ILogger logger,
                                    Exception ex,
                                    string message,
                                    params object[] args)
        {
            string msg;
            if (args.Length > 0)
            {
                msg = string.Format(message, args);
            }
            else
            {
                msg = message;
            }
            if (ex is not null)
            {
                msg += $"\n{ex}";
            }
            logger.LogError(msg);
        }
    }
}
