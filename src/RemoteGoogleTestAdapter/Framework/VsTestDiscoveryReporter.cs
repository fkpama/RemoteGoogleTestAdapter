using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Remote.Adapter.Framework
{
    internal class VsTestDiscoveryReporter : ITestFrameworkReporter
    {
        private readonly ITestCaseDiscoverySink discoverySink;
        private readonly string? overrideSource;

        public VsTestDiscoveryReporter(ITestCaseDiscoverySink sink,
                                       string? overrideSource = null)
        {
            this.discoverySink = sink;
            this.overrideSource = overrideSource;
        }
        public void ReportTestResults(IEnumerable<TestResult> testResults)
        {
            throw new NotImplementedException();
        }

        public void ReportTestsFound(IEnumerable<TestCase> testCases)
        {
            var tests = testCases.Select(x => x.ToVsTestCase(overrideSource));
            foreach(var test in tests)
                this.discoverySink.SendTestCase(test);
        }

        public void ReportTestsStarted(IEnumerable<TestCase> testCases)
        {
            throw new NotImplementedException();
        }
    }
}
