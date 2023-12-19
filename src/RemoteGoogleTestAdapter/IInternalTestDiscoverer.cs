using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter.Remote.Adapter
{
    internal interface IInternalTestDiscoverer
    {
        ICollection<TestCase> DiscoverTests(IEnumerable<string> sources,
                                            AdapterSettings settings,
                                            ISourceDeployment deployment,
                                            ILogger log,
                                            CancellationToken cancellationToken);
    }
}