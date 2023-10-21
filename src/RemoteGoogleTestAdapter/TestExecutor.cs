using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace RemoteGoogleTestAdapter
{
    [ExtensionUri(ExecutorUri)]
    public class TestExecutor : ITestExecutor2
    {
        internal const string ExecutorUri = "executor://remotegoogletest/1.0";
        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<TestCase>? tests,
                             IRunContext? runContext,
                             IFrameworkHandle? frameworkHandle)
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<string>? sources,
                             IRunContext? runContext,
                             IFrameworkHandle? frameworkHandle)
        {
            throw new NotImplementedException();
        }

        public bool ShouldAttachToTestHost(IEnumerable<string>? sources,
                                           IRunContext runContext)
        {
            throw new NotImplementedException();
        }

        public bool ShouldAttachToTestHost(IEnumerable<TestCase>? tests,
                                           IRunContext runContext)
        {
            throw new NotImplementedException();
        }
    }
}