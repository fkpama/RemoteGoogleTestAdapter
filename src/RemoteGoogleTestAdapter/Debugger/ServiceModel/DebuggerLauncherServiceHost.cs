using System.ServiceModel;

namespace GoogleTestAdapter.Remote.Adapter.Debugger.ServiceModel
{
    internal class DebuggerLauncherServiceHost : ServiceHost, IDebuggerHost
    {
        public DebuggerLauncherServiceHost(Guid id, IDebuggerLauncher instance, ILogger logger)
            : base(new DebuggerLauncherService(instance, logger),
                  new Uri[] { DebuggerProxyUtils.ConstructPipeUri(id) })
        {
            AddServiceEndpoint(typeof(IDebuggerLauncher),
                               new NetNamedPipeBinding(),
                               DebuggerProxyUtils.InterfaceAddress);
        }
    }
}
