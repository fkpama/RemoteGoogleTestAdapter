using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Remote.Adapter.Framework
{
    internal class VsTestExecutionReporter : ITestFrameworkReporter
    {
        private readonly IFrameworkHandle? framework;

        public VsTestExecutionReporter(IFrameworkHandle? framework)
        {
            this.framework = framework;
        }
        public void ReportTestResults(IEnumerable<TestResult> testResults)
        {
            if (this.framework is null) return;
            foreach (var result in testResults)
            {
                this.framework.RecordResult(result.ToVsTestResult());
            }
        }

        public void ReportTestsFound(IEnumerable<TestCase> testCases)
        {
            throw new NotImplementedException();
        }

        public void ReportTestsStarted(IEnumerable<TestCase> testCases)
        {
            if (this.framework is null) return;
            foreach(var test in testCases)
            {
                this.framework.RecordStart(test.ToVsTestCase());
            }
        }
    }
}
