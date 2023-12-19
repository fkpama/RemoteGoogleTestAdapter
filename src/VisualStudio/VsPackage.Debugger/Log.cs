using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using GoogleTestAdapter.Remote.VsPackage.Debugger;
using Microsoft.VisualStudio.Utilities;
using Sodiware.VisualStudio.Logging;

namespace VsPackage.Debugger
{
    internal static class Log
    {
        readonly static Guid TestOutputWindowGuid = new("{B85579AA-8BE0-4C4F-A850-90902B317581}");
        //readonly static Guid TestOutputWindowGuid = new("{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}");
        
    //public const string guidLinuxApplicationType = "02e62848-c147-6f63-57b7-446a314e7f59";

        static  ILogger? s_logger;
        internal static bool DebugMode
        {
            get;
            set;
        }
        public static ILogger TestOutputPaneLogger
        {
            get
            {
                if (s_logger is null)
                {

                    s_logger = Logger.OutputWindow(TestOutputWindowGuid,
                                                   Resources.OutputWindowPaneTitle,
                                                   new TestWindowLogFormatter());
                }
                return s_logger;
            }
        }

        internal static void DebugWarning(this ILogger logger, string message)
        {
            // TODO: check if debug mode
            logger.LogWarning(message);
        }

        internal static void DebugError(this ILogger logger, string message)
        {
            // TODO: check if debug mode
            logger.LogError(message);
        }

        internal static void DebugInfo(this ILogger logger, string message)
        {
            // TODO: check if debug mode
            logger.LogInformation(message);
        }

        sealed class TestWindowLogFormatter : ILogFormatter
        {
            public string Format(LogLevel level, string message, DateTimeOffset dt)
            {
                var pooled = PooledStringBuilder.GetInstance();
                try
                {
                    var stringBuilder = pooled.Builder;
                    stringBuilder.Clear();
                    if (true) // TODO: Only if test windows is in diag mode
                    {
                        stringBuilder.Append('[');
                        stringBuilder.Append(dt.ToString("d", (IFormatProvider)CultureInfo.CurrentCulture));
                        stringBuilder.Append(' ');
                        stringBuilder.Append(dt.ToString("h:mm:ss.fff tt", (IFormatProvider)CultureInfo.CurrentCulture));
                        stringBuilder.Append(']');
                        stringBuilder.Append(' ');

                        switch (level)
                        {
                            case LogLevel.Error:
                                stringBuilder.Append(Resources.ErrorLabel);
                                break;
                            case LogLevel.Warning:
                                stringBuilder.Append(Resources.WarningLabel);
                                break;
                        }
                        stringBuilder.Append(' ');
                    }

                    stringBuilder.Append(message);
                    return stringBuilder.ToString();
                }
                finally
                {
                    pooled.Free();
                }
            }
        }
    }
}
