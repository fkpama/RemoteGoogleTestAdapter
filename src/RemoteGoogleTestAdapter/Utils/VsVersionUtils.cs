namespace RemoteGoogleTestAdapter.Utils
{
    static class VsVersionUtils
    {
        public static Process? GetVisualStudioProcess(ILogger? logger)
        {
            var process = Process.GetCurrentProcess();
            try
            {
                if (process is null)
                {
                    return null;
                }
                string? executable = Path.GetFileName(process.MainModule.FileName).Trim().ToUpperInvariant();
                while (process is not null
                    && !string.Equals(executable, "DEVENV.EXE", StringComparison.Ordinal))
                {
                    process = ParentProcessUtils.GetParentProcess(process.Id);
                    executable = process is not null
                        ? Path.GetFileName(process.MainModule.FileName).Trim().ToUpperInvariant()
                        : null;
                }
            }
            catch (Exception e)
            {
                //logger?.LogInfo(String.Format(Resources.VSVersionMessage, e.Message));
                logger?.LogInfo($"Error getting VS PID: {e.Message}");
            }

            if (process is not null)
            {
                logger?.DebugInfo($"Found Visual studio process ID {process.Id}");
            }
            return process;
        }
    }

}
