using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.TestResults;

namespace GTestAdapter.Core.Tests
{
    public class StreamingTestParserTests
    {
        [Test]
        public void MyTestMethod()
        {
            const string text = """"
[==========] Running 1 test from 1 test suite.
[----------] Global test environment set-up.
[----------] 1 test from NestedNamespaceExampleFixture
[ RUN      ] NestedNamespaceExampleFixture.nested_fixture_example1
[       OK ] NestedNamespaceExampleFixture.nested_fixture_example1 (0 ms)
[----------] 1 test from NestedNamespaceExampleFixture (0 ms total)
[----------] Global test environment tear-down
[==========] 1 test from 1 test suite ran. (1 ms total)
[  PASSED  ] 1 test.
[Inferior 1 (process 27454) exited normally]
"""";
            var testCase = new TestCase("NestedNamespaceExampleFixture.nested_fixture_example1", "NestedNamespaceExampleFixture.nested_fixture_example1", "", "", "", 0);
            var reporter = new Mock<ITestFrameworkReporter>();
            var logger = new Mock<ILogger>();
            var sut = new StreamingStandardOutputTestResultParser(
                new[]{testCase},
                logger.Object,
                reporter.Object);
            foreach(var line in text.Split('\n'))
            {
                sut.ReportLine(line);
            }
        }
    }
}
