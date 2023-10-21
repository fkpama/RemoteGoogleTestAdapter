using System.Globalization;
using GoogleTestAdapter.Framework;
using GTestAdapter.Core;
using GTestAdapter.Core.Binary;
using GTestAdapter.Core.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using RemoteGoogleTestAdapter.Framework;
using RemoteGoogleTestAdapter.IDE;
using RemoteGoogleTestAdapter.Logging;
using RemoteGoogleTestAdapter.Settings;
using RemoteGoogleTestAdapter.Utils;
using RemoteGoogleTestAdapter.VisualStudio;

namespace RemoteGoogleTestAdapter
{

    //[FileExtension("")]
    //[FileExtension(".out")]
    [DefaultExecutorUri(TestExecutor.ExecutorUri)]
    public class TestDiscoverer : ITestDiscoverer
    {
        void ITestDiscoverer.DiscoverTests(IEnumerable<string> sources,
                                           IDiscoveryContext discoveryContext,
                                           IMessageLogger messageLogger,
                                           ITestCaseDiscoverySink discoverySink)
        {
            VsTestFrameworkAdapterSettings settings = null!;
            var logger = new VsTestFrameworkLogger(messageLogger,
                () => settings!.DebugMode,
                () => settings!.TimestampOutput);
            settings = new VsTestFrameworkAdapterSettings(discoveryContext?.RunSettings, logger);
            if (settings.DebugMode)
                AdapterUtils.WaitForDebugger(logger);
            var loggerFactory = new VsTestFrameworkLoggerFactory(messageLogger);
            logger.LogInfo(Resources.TestDiscoveryStarting);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                string? overrideSource = settings.OverrideSource;
                if (!string.IsNullOrWhiteSpace(overrideSource))
                {
                    logger.LogInfo($"Overriding test source discovery: {settings.OverrideSource}");
                    var original = sources.FirstOrDefault();
                    Assumes.NotNullOrEmpty(overrideSource);
                    sources = new[] { overrideSource };
                    overrideSource = original;
                }

                var process = VsVersionUtils.GetVisualStudioProcess(logger);
                ISourceDeployment sourceDeployment = null!;
                if (process is not null
                    && VsIde.TryGetInstance(process.Id, logger, out var vsIde))
                {
                    Assumes.NotNull(vsIde);
                    sourceDeployment = new VsSourceDeployment(vsIde,
                                                              settings,
                                                              loggerFactory.CreateLogger<VsSourceDeployment>());
                }
                else
                {
                    var client = getSshClient(cancellationToken);
                    sourceDeployment = new SourceDeployment(client, loggerFactory);
                }

                Assumes.NotNull(sourceDeployment);

                var reporter = new VsTestFrameworkReporter(discoverySink, overrideSource);
                var resolver = settings.CollectSourceInformation
                ? new TestLocationResolver(sourceDeployment, loggerFactory.CreateLogger<TestLocationResolver>())
                : null;

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                DiscoverTestsAsync(sources,
                                   settings,
                                   sourceDeployment,
                                   reporter,
                                   resolver,
                                   loggerFactory)
                    .GetAwaiter()
                    .GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

                stopwatch.Stop();
                logger.LogInfo(string.Format(CultureInfo.CurrentUICulture,
                                                      Resources.TestDiscoveryCompleted,
                                                      stopwatch.Elapsed));
            }
            catch (Exception ex)
            {
                logger.LogError(string.Format(CultureInfo.CurrentUICulture,
                                                      Resources.TestDiscoveryExceptionError,
                                                      ex));
            }
        }
        public Task DiscoverTestsAsync(IEnumerable<string> sources,
                                       AdapterSettings settings,
                                       ISourceDeployment deployment,
                                       ITestFrameworkReporter reporter,
                                       ILoggerFactory? loggerFactory = null)
            => DiscoverTestsAsync(sources,
                                  settings,
                                  deployment,
                                  reporter,
                                  new TestLocationResolver(deployment),
                                  loggerFactory.Safe());
        public async Task DiscoverTestsAsync(IEnumerable<string> sources,
                                             AdapterSettings settings,
                                             ISourceDeployment deployment,
                                             ITestFrameworkReporter reporter,
                                             ITestLocationResolver? testLocationResolver,
                                             ILoggerFactory loggerFactory)
        {
            var tasks = new List<Task>();
            CancellationToken cancellationToken = default;
            foreach (var source in sources)
            {
                tasks.Add(createDiscoveryContextAsync(source,
                                                      deployment,
                                                      settings,
                                                      reporter,
                                                      testLocationResolver,
                                                      loggerFactory,
                                                      cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private ISshClient getSshClient(CancellationToken cancellationToken)
        {
            var client = ConnectionFactory.CreateClient(VsIde.JoinableTaskFactory, cancellationToken);
            return client;
        }

        private async Task createDiscoveryContextAsync(
            string source,
            ISourceDeployment deployment,
            AdapterSettings settings,
            ITestFrameworkReporter reporter,
            ITestLocationResolver? locationResolver,
            ILoggerFactory factory,
            CancellationToken cancellationToken)
        {
            if (deployment.IsGoogleTestBinary(source, out var binary))
            {
                Assumes.NotNull(binary);
                var discovery = new TestDiscoveryContext(binary,
                                                         source,
                                                         settings,
                                                         reporter,
                                                         factory.CreateLogger<TestDiscoveryContext>());
                var provider = new TestProvider(deployment,
                                                binary,
                                                source,
                                                factory.CreateLogger<TestProvider>());
                try
                {
                    await discovery.DiscoverTests(provider,
                                                  locationResolver,
                                                  cancellationToken)
                        .ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    factory
                        .CreateLogger<TestDiscoverer>()
                        .LogError(ex, "Test discovery error: {msg}", ex.Message);
                }
            }
        }
    }
}