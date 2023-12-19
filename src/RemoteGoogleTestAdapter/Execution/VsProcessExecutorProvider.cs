using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Remote.Adapter.VisualStudio;
using GoogleTestAdapter.Remote.Execution;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Settings;

namespace GoogleTestAdapter.Remote.Adapter.Execution
{
    internal sealed class VsProcessExecutorProvider : ProcessExecutorProvider
    {
        private readonly AdapterSettings settings;
        private readonly Func<VsIde?> ide;

        public VsProcessExecutorProvider(AdapterSettings settings,
                                         Func<VsIde?> ide,
                                         ISourceDeployment deployment,
                                         ISshClientRegistry sshClientRegistry,
                                         ILogger logger)
            : base(deployment, sshClientRegistry, logger)
        {
            this.settings = settings;
            this.ide = ide;
        }
    }
}
