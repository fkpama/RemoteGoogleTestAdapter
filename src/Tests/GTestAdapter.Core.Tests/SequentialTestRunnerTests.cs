using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Remote.Adapter;
using GoogleTestAdapter.Remote.Adapter.Settings;
using GoogleTestAdapter.Remote.Adapter.Utils;
using GoogleTestAdapter.Remote.Execution;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Runners;

namespace GTestAdapter.Core.Tests
{
    internal class SequentialTestRunnerTests : GtestCompiler
    {
        VsTestFrameworkAdapterSettings settings;
        Mock<ITestFrameworkReporter> reporter;
        Mock<ISourceDeployment> sourceDeployment;
        Mock<IProcessExecutorProvider> executorProvider;
        Mock<IDebuggedProcessLauncher> debugProcessLauncher;
        Mock<ILogger> logger;
        SequentialRemoteTestRunner sut;

        public SequentialTestRunnerTests()
        {
            this.settings  = null!;
            this.reporter  = null!;
            this.sourceDeployment  = null!;
            this.executorProvider  = null!;
            this.logger  = null!;
            this.sut  = null!;
            this.debugProcessLauncher = null!;
        }

        [SetUp]
        public void SetUp()
        {
            this.settings = new();
            this.reporter  = new();
            this.sourceDeployment  = new();
            this.executorProvider  = new();
            this.logger  = new();
            this.debugProcessLauncher = new();
            this.sut = new(reporter.Object,
                           settings,
                           sourceDeployment.Object,
                           executorProvider.Object,
                           logger.Object);
        }

        private async Task<TestCase[]> getTests()
        {
            var compile = await this.CompileTestAsync();
            var discoverer = new TestDiscoverer();
            var collector = new TestCaseDiscoveryCollector();
            var deployment = new Mock<ISourceDeployment>();
            deployment.HasTestBinary(compile, DefaultTestOutput);
            await discoverer.DiscoverTestsAsync(new[] { compile.OutputFilename },
                settings,
                deployment.Object,
                collector,
                new Mock<ILogger>().Object);

            Assert.That(collector.Tests.Count(), Is.EqualTo(1));
            return collector.Tests.Select(x => x.ToTestCase()).ToArray();
        }

        [Test]
        public async Task can_run_tests()
        {
            var testCases = await getTests();
            await sut.RunTestsAsync(testCases,
                                    null,
                                    null,
                                    cancellationToken);
        }
        [Test]
        public async Task can_debug_tests()
        {
            var testCases = await getTests();
            await sut.RunTestsAsync(testCases,
                                    null,
                                    null,
                                    cancellationToken);
        }
    }
}
