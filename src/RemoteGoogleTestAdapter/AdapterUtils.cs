namespace RemoteGoogleTestAdapter
{
    internal static class AdapterUtils
    {
        internal static readonly char[] LineSeparatorChars = new[]{'\r', '\n'};
        internal static void WaitForDebugger(ILogger logger)
        {
            var env = Environment.GetEnvironmentVariable("GTEST_WAIT_FOR_DEBUGGER");
            if (env.IsPresent())
            {
                var countdown = new Stopwatch();
                while (!Debugger.IsAttached && countdown.Elapsed < TimeSpan.FromMinutes(1))
                {
                    logger.DebugInfo($"Waiting for debugger. PID: {Process.GetCurrentProcess().Id}");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}
