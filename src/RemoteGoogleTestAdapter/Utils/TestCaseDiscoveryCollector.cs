using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Remote.Adapter.Utils
{
    internal class TestCaseDiscoveryCollector : ITestFrameworkReporter
    {
        private readonly List<TestCase> testCases = new();
        private readonly string? overrideSource;

        public ICollection<VsTestCase> Tests => this
            .testCases
            .Select(x => x.ToVsTestCase(this.overrideSource))
            .ToArray();

        public TestCaseDiscoveryCollector(string? overrideSource = null)
        {
            this.overrideSource = overrideSource;
        }
        public void ReportTestResults(IEnumerable<TestResult> testResults)
        {
            throw new NotImplementedException();
        }

        public void ReportTestsFound(IEnumerable<TestCase> testCases)
        {
            this.testCases.AddRange(testCases);
        }

        public void ReportTestsStarted(IEnumerable<TestCase> testCases)
        {
            throw new NotImplementedException();
        }
    }
}
