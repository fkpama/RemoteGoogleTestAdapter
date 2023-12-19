using System.Globalization;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Remote.Adapter.Framework;
using GoogleTestAdapter.Remote.Adapter.Utils;
using GoogleTestAdapter.Remote.Adapter.VisualStudio;
using GoogleTestAdapter.Remote.Discovery;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Settings;
using GoogleTestAdapter.Remote.Symbols;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Remote.Adapter
{

    //[FileExtension("")]
    //[FileExtension(".out")]
    [DefaultExecutorUri(TestExecutor.ExecutorUri)]
    public sealed class TestDiscoverer : ITestDiscoverer, IInternalTestDiscoverer
    {
        private const AdapterMode mode = AdapterMode.Discovery;

        public TestDiscoverer()
        {
        }

        void ITestDiscoverer.DiscoverTests(IEnumerable<string> sources,
                                           IDiscoveryContext discoveryContext,
                                           IMessageLogger messageLogger,
                                           ITestCaseDiscoverySink discoverySink)
        {
            AdapterSettings? settings = null;
            ILogger? logger = null;
            try
            {
                AdapterUtils.Initialize(discoveryContext,
                                        out settings,
                                        messageLogger,
                                        out logger,
                                        out var loggerFactory);
                if (settings.DebugMode)
                    AdapterUtils.WaitForDebugger(mode, logger);

                sources = AdapterUtils.GetSources(sources,
                                                  settings,
                                                  out var originalSource,
                                                  logger);
                logger.LogInfo(string.Format(Resources.TestDiscoveryStarting, settings.CollectSourceInformation ? " (Source infos: true)" : null));
                CancellationToken cancellationToken = CancellationToken.None;

                var sourceDeployment = VsVersionUtils
                    .GetDeployment(settings,
                                   logger,
                                   loggerFactory,
                                   cancellationToken);

                var reporter = new VsTestDiscoveryReporter(discoverySink, originalSource);
                var resolver = settings.CollectSourceInformation
                ? new TestLocationResolver(sourceDeployment, loggerFactory.CreateLogger<TestLocationResolver>())
                : null;

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                DiscoverTestsAsync(sources,
                                   settings,
                                   sourceDeployment,
                                   reporter,
                                   resolver,
                                   //loggerFactory,
                                   logger,
                                   cancellationToken)
                    .GetAwaiter()
                    .GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            }
            catch (Exception ex)
            {
                var msg1 = string.Format(CultureInfo.CurrentUICulture,
                                                      Resources.TestDiscoveryExceptionError,
                                                      ex);
                messageLogger.SendMessage(TestMessageLevel.Error, $"{msg1}\n{ex}");
            }
            finally
            {
                settings?.ExecuteCleanups(logger);
            }
        }
        public Task DiscoverTestsAsync(IEnumerable<string> sources,
                                       AdapterSettings settings,
                                       ISourceDeployment deployment,
                                       ITestFrameworkReporter reporter,
                                       ILogger log,
                                       CancellationToken cancellationToken = default)
            => DiscoverTestsAsync(sources,
                                  settings,
                                  deployment,
                                  reporter,
                                  new TestLocationResolver(deployment),
                                  //loggerFactory.Safe(),
                                  log,
                                  cancellationToken);

        ICollection<TestCase> IInternalTestDiscoverer.DiscoverTests(
            IEnumerable<string> sources,
            AdapterSettings settings,
            ISourceDeployment deployment,
            //ILoggerFactory? loggerFactory,
            ILogger log,
            CancellationToken cancellationToken)
        {
            var collection = new TestCaseDiscoveryCollector();
            ITestLocationResolver? testLocationResolver = settings.CollectSourceInformation
                ? new TestLocationResolver(deployment)
                : null;
            VsIde.JoinableTaskFactory.Run(() => DiscoverTestsAsync(sources,
                                  settings,
                                  deployment,
                                  collection,
                                  testLocationResolver,
                                  log,
                                  cancellationToken));
            return collection.Tests;
        }
        public async Task DiscoverTestsAsync(IEnumerable<string> sources,
                                             AdapterSettings settings,
                                             ISourceDeployment deployment,
                                             ITestFrameworkReporter reporter,
                                             ITestLocationResolver? testLocationResolver,
                                             //ILoggerFactory loggerFactory,
                                             ILogger log,
                                             CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            foreach (var source in sources)
            {
                tasks.Add(createDiscoveryContextAsync(source,
                                                      deployment,
                                                      reporter,
                                                      testLocationResolver,
                                                      log,
                                                      cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task createDiscoveryContextAsync(
            string source,
            ISourceDeployment deployment,
            ITestFrameworkReporter reporter,
            ITestLocationResolver? locationResolver,
            //ILoggerFactory factory,
            ILogger log,
            CancellationToken cancellationToken)
        {
            if (deployment.IsGoogleTestBinary(source, out var binary))
            {
                Assumes.NotNull(binary);
                var discovery = new TestDiscoveryContext(source, reporter, log);
                var provider = new TestProvider(deployment, binary, source, log);
                                                //factory.CreateLogger<TestProvider>());
                try
                {
                    await discovery.DiscoverTests(provider,
                                                  locationResolver,
                                                  cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"Test discovery error: {ex.Message}");
                }
            }
        }
    }
}