using System.ServiceModel;

namespace GoogleTestAdapter.Remote.Adapter.Debugger.ServiceModel
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    internal class DebuggerLauncherService : IDebuggerLauncher
    {
        private readonly IDebuggerLauncher instance;
        private readonly ILogger log;

        internal DebuggerLauncherService(IDebuggerLauncher instance, ILogger log)
        {
            this.instance = instance;
            this.log = log;
        }

        public void ClientConnected()
        {
            try
            {
                this.instance.ClientConnected();
            }
            catch(Exception ex)
            {
                Throw($"Failed to connect client to server: {ex.Message}");
            }
        }

        public void ErrorReceived(string errorText)
        {
            try
            {
                this.instance.ErrorReceived(errorText);
            }
            catch (Exception e)
            {
                Throw($"Exception occured {nameof(ErrorReceived)}: {e.Message}");
            }
        }

        public void OutputReceived(string outputText)
        {
            try
            {
                this.instance.OutputReceived(outputText);
            }
            catch (Exception e)
            {
                Throw($"Exception occured {nameof(OutputReceived)}: {e.Message}");
            }
        }

        public void ReportTestResults(TestResultModel[] results)
        {
            try
            {
                this.instance.ReportTestResults(results);
            }
            catch (Exception e)
            {
                Throw($"Exception occured {nameof(ReportTestResults)}: {e.Message}");
            }
        }

        public void ReportTestStarted(string[] tests)
        {
            try
            {
                this.instance.ReportTestStarted(tests);
            }
            catch (Exception e)
            {
                Throw($"Exception occured {nameof(ReportTestStarted)}: {e.Message}");
            }
        }

        public void Shutdown(DebuggerShutdownReason reason)
        {
            try
            {
                this.instance.Shutdown(reason);
            }
            catch (Exception e)
            {
                log?.LogError(e, $"Failed to shutdown test adapter: {e.Message}");
            }
        }

        private void Throw(string message)
        {
            throw new FaultException<DebuggerFault>(new DebuggerFault(message));
        }
    }
}
