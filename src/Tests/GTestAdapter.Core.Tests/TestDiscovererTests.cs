using DebugLibrary.Tests.Utils;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Remote.Adapter;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Settings;

namespace GTestAdapter.Core.Tests
{
    public class TestDiscovererTests  : GtestCompiler
    {
        const string ExampleMethodSource = @"#include <gtest/gtest.h>
TEST(Example, method) { };";
        const string ExampleMethodOutput =@"Example.
  method";
        TestDiscoverer sut;
        AdapterSettings settings;
        Mock<ISourceDeployment> mockDeployment;
        Mock<ITestFrameworkReporter> mockReporter;
        //readonly ILoggerFactory nullLoggerFactory = NullLoggerFactory.Instance;
        readonly Mock<ILogger> logger = new();

        public TestDiscovererTests()
        {
            this.sut = null!;
            this.settings = null!;
            this.mockDeployment = null!;
            this.mockReporter = null!;
            //this.nullLoggerFactory = null!;
        }

        [SetUp]
        public void Setup()
        {
            settings = new AdapterSettings();
            mockDeployment = new Mock<ISourceDeployment>();
            mockReporter = new Mock<ITestFrameworkReporter>();
            //loggerFactory = NullLoggerFactory.Instance;
            sut = new();
        }

        [Test]
        public async Task send_TestCaseMetadataProperty()
        {
            mockReporter.ExpectSingleTestCase(x => x.Properties.OfType<TestCaseMetaDataProperty>().Any());
            var result = await this.Setup(ExampleMethodSource, ExampleMethodOutput);

            await DiscoverTests(result);

            mockReporter.VerifyAll();
        }

        async Task DiscoverTests(CompilationResult result)
        {
            await sut.DiscoverTestsAsync(new[] { result.OutputFilename },
                                                settings,
                                                mockDeployment.Object,
                                                mockReporter.Object,
                                                logger.Object);
        }

        [Test]
        public async Task can_find_tests()
        {
            const string src = @"#include <gtest/gtest.h>

namespace NS1 {
TEST(Example, method)
{
}
};
";
            string output = @"
Example.
  method
";

            var result = await this.Setup(src, output);
            mockReporter.ExpectSingleTestCase("Example.method");

            await DiscoverTests(result);

            mockDeployment.Verify();
            mockReporter.Verify();
        }

        private async Task<CompilationResult> Setup(string src, string output)
        {
            var result = await this.CompileTestAsync(src);
            mockDeployment.HasTestListOutput(output.Trim().Split('\n'));
            mockDeployment.HasTestBinary(result);
            return result;
        }

        [Test]
        public async Task duplicates()
        {
            const string src = @"#include <gtest/gtest.h>


namespace NS1 {
class NS1_Example : public ::testing::Test { };
TEST_F(NS1_Example, method)
{
}
}

namespace NS2 {
class NS2_Example : public ::testing::Test { };
TEST_F(NS2_Example, method)
{
}
}
";

            string[] output = @"
Example.
  method
".Trim().Split('\n');

            var result = await this.CompileTestAsync(src);
            await sut.DiscoverTestsAsync(new[] {result.OutputFilename },
                                                settings,
                                                mockDeployment.Object,
                                                mockReporter.Object,
                                                logger.Object);

            mockDeployment.Verify();
            mockReporter.Verify();
        }
    }
}