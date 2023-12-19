using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Adapter.Utils;
using GoogleTestAdapter.Remote.Settings;

namespace GoogleTestAdapter.Remote.Adapter.VisualStudio
{
    static class VsVersionUtils
    {
        public static Process? GetVisualStudioProcess(ILogger? logger)
        {
            if (SysDebugger.IsAttached)
            {
                // we are alreay launched by vs.
                return null;
            }
            var process = Process.GetCurrentProcess();
            try
            {
                if (process is null)
                {
                    return null;
                }
                var executable = Path.GetFileName(process.MainModule.FileName).Trim().ToUpperInvariant();
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

        internal static ISourceDeployment GetDeployment(AdapterSettings settings,
                                                        ILogger logger,
                                                        ILoggerFactory loggerFactory,
                                                        CancellationToken cancellationToken)
            => GetDeployment(settings,
                             logger,
                             loggerFactory,
                             out _,
                             cancellationToken);
        internal static ISourceDeployment GetDeployment(AdapterSettings settings,
                                                        ILogger logger,
                                                        ILoggerFactory loggerFactory,
                                                        out ISshClientRegistry registry,
                                                        CancellationToken cancellationToken)
        {

            var process = GetVisualStudioProcess(logger);
            ISourceDeployment sourceDeployment;
            if (process is null)
            {
                VsIde.CreateLocalTaskFactory();
            }

            var clientRegistry = new SshClientRegistry(cancellationToken,
                                                       settings,
                                                       (id, cancellation) => Task.FromResult(AdapterUtils.GetSshClient(id, cancellation)),
                                                       cancellation => Task.FromResult(AdapterUtils.GetSshClient(cancellation)));
            if (process is not null
                && VsIde.TryGetInstance(process.Id,
                                        clientRegistry,
                                        logger,
                                        out var vsIde))
            {
                Assumes.NotNull(vsIde);
                VsIde.SetCurrentInstance(vsIde);
                sourceDeployment = new VsSourceDeployment(vsIde,
                                                          settings,
                                                          clientRegistry,
                                                          logger,
                                                          loggerFactory);
            }
            else
            {
                sourceDeployment = new SourceDeployment(settings,
                                                        clientRegistry,
                                                        logger,
                                                        loggerFactory);
            }

            Assumes.NotNull(sourceDeployment);

            registry = clientRegistry;
            return sourceDeployment;
        }
    }

}
