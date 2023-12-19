using System.Runtime.CompilerServices;
using System.ServiceModel;
using GoogleTestAdapter.Remote.Debugger;
using LinuxDebugger.VisualStudio;
using LinuxDebugger.VisualStudio.Debugger;
using LinuxDebugger.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Sodiware.VisualStudio.Logging;
using VsPackage.Debugger;

namespace GoogleTestAdapter.Remote.VsPackage.Debugger.Client
{
    public sealed class DebuggerClientLauncher
    {
        private readonly IServiceProvider services;
        private ILogger? m_log;

        public IDebuggerLauncher Client { get; }
        public DebuggerLaunchParameters Parameters { get; }

        internal ILogger log
        {
            get
            {
                if (m_log is null)
                {
                    if (ThreadHelper.CheckAccess())
                    {
                        m_log = Log.TestOutputPaneLogger;
                    }
                    else
                    {
                        var tf =ThreadHelper.JoinableTaskFactory;
                        m_log = tf.Run(async () =>
                        {
                            await tf.SwitchToMainThreadAsync();
                            return Log.TestOutputPaneLogger;
                        });
                    }
                }
                return this.m_log;
            }
        }

        public DebuggerClientLauncher(IDebuggerLauncher client,
                                      DebuggerLaunchParameters parameters,
                                      IServiceProvider services)
        {
            this.Client = client;
            this.Parameters = parameters;
            this.services = services;
        }

        public static DebuggerClientLauncher LaunchFromJson(IServiceProvider services, string json)
        {
            var parameters= DebuggerLaunchParameters.Deserialize(json);
            if (parameters is null)
            {
                throw new NotImplementedException();
            }

            var serviceIdString = parameters.ServiceId;
            if (!Guid.TryParse(serviceIdString, out var serviceId))
            {
                throw new NotImplementedException();
            }
            // TODO: Take it from parameters
            var ts = TimeSpan.FromSeconds(30);
            var launcher = DebuggerClientFactory.Create(serviceId, ts);
            var client = new DebuggerClientLauncher(launcher, parameters, services);
            return client;
        }

        public async Task StartAsync()
        {
            // make sure we're in the background
            //await TaskScheduler.Default;
            log.LogInformation("Google test adapter client Debugger starting...");
            CancellationToken cancellationToken = default;
            List<DebugSession> sessions;
            try
            {
                sessions = await createDebugSessionsAsync(cancellationToken)
                    .ConfigureAwait(false);
                log.DebugInfo("Debug sessions successfully created");
                this.call(x => x.ClientConnected());
            }
            catch (Exception ex)
            {
                log.LogError($"Failed to create debug sessions: {ex.Message}");
                this.call(x => x.Shutdown(DebuggerShutdownReason.Error));
                return;
            }

            var sdm = new SessionDebugManager(sessions, log);
            bool shutdownCalled = false;
            sdm.OutputString += onOutputString;
            sdm.SessionEnded += (o, e) =>
            {
                shutdownCalled = true;
                this.call(x => x.Shutdown(DebuggerShutdownReason.NormalExit));
            };
            try
            {
                await sdm.StartAsync(cancellationToken).NoAwait();
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error during debugger launch: {ex.Message}");
                if (!shutdownCalled)
                {
                    this.call(x => x.Shutdown(DebuggerShutdownReason.Error));
                }
            }
        }

        private void onOutputString(object sender, OutputStringEventArgs e)
        {
            this.call(x => x.OutputReceived(e.Text));
        }

        private async Task<List<DebugSession>> createDebugSessionsAsync(
            CancellationToken cancellationToken)
        {
            var solution = this.services.GetService<SVsSolution, IVsSolution>();
            var dict = new Dictionary<string, DebugLauncher>(this.Parameters.Batches.Count, StringComparer.Ordinal);
            var sessions = new List<DebugSession>(this.Parameters.Batches.Count);
            foreach (var item in this.Parameters.Batches)
            {
                var projectName = item.ProjectId;
                if (!dict.TryGetValue(projectName, out var launcher))
                {
                    launcher = await createLauncherAsync(projectName,
                                                         item.ConnectionId,
                                                         solution,
                                                         cancellationToken).NoAwait();
                    dict.Add(projectName, launcher);
                }

                var session = new DebugSession(launcher,
                                               this.Client,
                                               projectName,
                                               item,
                                               solution,
                                               ThreadHelper.JoinableTaskFactory,
                                               log);
                sessions.Add(session);
            }
            return sessions;
        }

        private async Task<DebugLauncher> createLauncherAsync(string projectName,
            int connectionId,
            IVsSolution solution,
            CancellationToken cancellationToken)
        {
            IVsSshClient sshClient;
            Assumes.NotNull(solution);
            var connectionManager = VsConnectionManager.GlobalInstance;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (connectionId == 0)
            {
                sshClient = await connectionManager
                    .GetProjectConnectionAsync(projectName, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                sshClient = await connectionManager
                    .GetConnectionAsync(connectionId, cancellationToken)
                    .ConfigureAwait(false);

            }
            var settings = new LinuxDebuggerSettings
            {
                DebuggingType = LinuxConstants.DebuggingTypes.CplusPlus,
                //VsDbgDirectory = "/home/fred/vsdbg"
            };
            var launcher = new DebugLauncher(sshClient,
                                             settings,
                                             log);
            return launcher;
        }

        private int getProjectConnectionId(IVsHierarchy hier)
        {
            throw new NotImplementedException();
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void call(Action<IDebuggerLauncher> action)
            => call([DebuggerStepThrough](x) =>
            {
                action(x);
                return (object?)null;
            });
        private T call<T>(Func<IDebuggerLauncher, T> action)
        {
            try
            {
                return action(Client);
            }
            catch(FaultException ex)
            {
                string? details = null;
                if (ex is FaultException<DebuggerFault> ex1)
                {
                    details = ex1.Detail?.Message;
                }
                details ??= ex.Message;
                if (details.IsPresent())
                    this.showError(details);
                this.tryStopServer(DebuggerShutdownReason.Error);
                throw;
            }
        }

        private void tryStopServer(DebuggerShutdownReason reason)
        {
            try
            {
                this.Client.Shutdown(reason);
            }
            catch (FaultException ex1)
            {
                //var details = ex1.Reason;
                //this.showError(details.Message);
                throw;
            }
        }

        private void showError(string? message)
        {
            VsShellUtilities.ShowMessageBox(services,
                                            message,
                                            "Google Remote Tests Adapter",
                                            OLEMSGICON.OLEMSGICON_CRITICAL,
                                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
