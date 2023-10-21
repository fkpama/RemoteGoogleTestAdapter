using Castle.Core.Logging;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GTestAdapter.Core.Binary;
using GTestAdapter.Core.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RemoteGoogleTestAdapter;
using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core.Tests
{
    public class TestDiscovererTests  : GtestCompiler
    {
        [Test]
        public async Task can_find_tests()
        {
            const string src = @"#include <gtest/gtest.h>

TEST(Example, method)
{
}
";

            string[] output = @"
Example.
  method
".Trim().Split('\n');
            var discoverer = new TestDiscoverer();

            var settings = new AdapterSettings();
            var mockDeployment = new Mock<ISourceDeployment>();
            var mockReporter = new Mock<ITestFrameworkReporter>();
            var loggerFactory = NullLoggerFactory.Instance;

            mockDeployment.HasTestListOutput(output).Verifiable();
            mockReporter.ExpectSingleTestCase("Example.method").Verifiable();


            var result = await this.CompileTestAsync(src);
            await discoverer.DiscoverTestsAsync(new[] {result.OutputFilename },
                                                settings,
                                                mockDeployment.Object,
                                                mockReporter.Object,
                                                loggerFactory);

            mockDeployment.Verify();
            mockReporter.Verify();
        }
    }
}