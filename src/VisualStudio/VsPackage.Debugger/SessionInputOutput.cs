using System.ServiceModel;
using LinuxDebugger.VisualStudio.Debugger;
using Sodiware.VisualStudio.Logging;

namespace GoogleTestAdapter.Remote.VsPackage.Debugger
{
    internal sealed partial class DebugSession
    {
        sealed class SessionInputOutput : IProcessInputOutput
        {
            private readonly IDebuggerLauncher service;
            private readonly ILogger log;
            public bool IsConnected { get; private set; }

            public SessionInputOutput(IDebuggerLauncher service, ILogger log)
            {
                this.service = service;
                this.log = log;
            }

            public void Attach(IProcessInputOutputHandles inputStream)
            {
            }

            public void HandleOutput(string output)
            {
                try
                {
                    this.service.OutputReceived(output);
                }
                catch (FaultException<DebuggerFault> e)
                {
                    log.LogError($"Output processing error: {e.Detail.Message}");
                }
                catch (CommunicationObjectAbortedException e1) { }
                catch (CommunicationObjectFaultedException e1) { }
            }

            public void HansleError(string output)
            {
                try
                {
                    this.service.ErrorReceived(output);
                }
                catch (FaultException<DebuggerFault> e)
                {
                    log.LogError($"Error processing error: {e.Detail.Message}");
                }
                catch (CommunicationObjectAbortedException e1) { }
                catch (CommunicationObjectFaultedException e1) { }
            }

            internal void Disconneded()
            {
                this.IsConnected = false;
            }
        }
    }
}
