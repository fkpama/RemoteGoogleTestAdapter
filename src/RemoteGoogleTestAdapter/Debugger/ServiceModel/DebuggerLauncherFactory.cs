namespace GoogleTestAdapter.Remote.Adapter.Debugger.ServiceModel
{

    internal interface IDebuggerLauncherFactory
    {
        IDebuggerHost Open(Guid guid, IDebuggerLauncher instance, ILogger logger);
    }

    internal interface IDebuggerHost : IDisposable
    {
        void Close();
    }

    internal sealed class DebuggerServiceFactory : IDebuggerLauncherFactory
    {
        public IDebuggerHost Open(Guid guid, IDebuggerLauncher instance, ILogger log)
        {
            var host = new DebuggerLauncherServiceHost(guid, instance, log);
            host.Open();
            return host;
        }
    }
}
