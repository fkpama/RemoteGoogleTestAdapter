using System.Globalization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Remote.Adapter.Utils
{
    internal static class LocUtils
    {
        public static void TimestampMessage(ref string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            message = $"{timestamp} - {message ?? ""}";
        }
        public static Severity GetSeverity(this TestMessageLevel level)
        {
            switch (level)
            {
                case TestMessageLevel.Informational:
                    return Severity.Info;
                case TestMessageLevel.Warning:
                    return Severity.Warning;
                case TestMessageLevel.Error:
                    return Severity.Error;
                default:
                    throw new InvalidOperationException(String.Format(Resources.UnknownLiteral, level));
            }
        }

    }
}
