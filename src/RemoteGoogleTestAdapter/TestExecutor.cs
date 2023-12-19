using GoogleTestAdapter.Remote.Adapter.Execution;
using GoogleTestAdapter.Remote.Adapter.Framework;
using GoogleTestAdapter.Remote.Adapter.Traits;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Remote.Adapter;

[ExtensionUri(ExecutorUri)]
public partial class TestExecutor : ITestExecutor2
{
    internal const string ExecutorUri = "executor://remotegoogletest/1.0";
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly ITestExecutorShim shim;
    private bool cancelled;
    private ILogger? logger;

    private const AdapterMode mode = AdapterMode.Execution;

    public TestExecutor()
        : this(new TestExecutorShim())
    {
    }
    internal TestExecutor(ITestExecutorShim shim)
    {
        this.shim = shim;
    }

    public void Cancel()
    {
        if (cancelled)
        {
            return;
        }
        logger.LogInfo("Execution cancellation requested.");
        this.cancelled = true;
        cancellationTokenSource.Cancel();
    }

    public void RunTests(IEnumerable<VsTestCase>? testEnum,
                         IRunContext? runContext,
                         IFrameworkHandle? frameworkHandle)
    {
        if (testEnum is null)
        {
            // TODO
            return;
        }

        this.shim.Initialize(runContext,
                                out var settings,
                                frameworkHandle,
                                out this.logger,
                                out var loggerFactory);
        settings.DiscoveryMode = AdapterMode.Discovery;
        if (settings.DebugMode)
            AdapterUtils.WaitForDebugger(mode, logger);

        if (!ableToRun(settings, logger))
            return;

        try
        {
            ICollection<VsTestCase> tests = testEnum.ToList();
            tests = this.filterTests(tests, runContext, logger);
            if (tests.Count == 0) return;

            var deployment = this.shim.GetDeployment(settings,
                           logger,
                           loggerFactory,
                           out var registry,
                           this.cancellationTokenSource.Token);

            RunTests(tests,
                     settings,
                     frameworkHandle,
                     deployment,
                     registry,
                     logger);
        }
        finally
        {
            settings?.ExecuteCleanups(logger);
        }
    }

    private bool ableToRun(AdapterSettings settings, ILogger logger)
    {
        if (settings.IsBeingDebugged && settings.DebuggerPipeId.IsMissing())
        {
            //logger.LogError("Unable to start a debugging session: Debugger pipe id missing");
            //return false;
        }
        return true;
    }

    private ICollection<VsTestCase> filterTests(ICollection<VsTestCase> testCases,
        IRunContext? runContext,
        ILogger logger)
    {
        if (runContext is null)
        {
            return testCases;
        }
        logger.DebugInfo($"Starting test filtering (count: {testCases.Count})");
        var filter = new TestCaseFilter(runContext, testCases, logger);
        testCases = filter.Filter(testCases).ToList();
        logger.DebugInfo($"Number of test remaining after filtering: {testCases.Count}");
        return testCases;
    }

    internal void RunTests(ICollection<VsTestCase> testCases,
                           AdapterSettings settings,
                           IFrameworkHandle? framework,
                           ISourceDeployment deployment,
                           ISshClientRegistry registry,
                           ILogger logger)
    {
        logger.LogInfo(Resources.TestExecutionStarting);
        logger.DebugInfo($"Executing tests:\n  {string.Join("\n  ", testCases.Select(x => x.DisplayName))}");

        try
        {
            var reporter = new VsTestExecutionReporter(framework);
            var executorProvider = new VsProcessExecutorProvider(settings,
                                                             this.shim.GetVsIde,
                                                             deployment,
                                                             registry,
                                                             logger);
            var testRunner = this.shim.SelectRunner(settings,
                                                reporter,
                                                executorProvider,
                                                deployment,
                                                logger,
                                                cancellationTokenSource.Token);

            testRunner.RunTests(testCases, settings.IsBeingDebugged);
        }
        finally
        {
            settings.ExecuteCleanups(logger);
        }
    }

    public void RunTests(IEnumerable<string>? sources,
                         IRunContext? runContext,
                         IFrameworkHandle? frameworkHandle)
    {
        this.shim.Initialize(runContext,
                             out var settings,
                             frameworkHandle,
                             out this.logger,
                             out var loggerFactory);
        if (sources is null)
        {
            logger.LogError($"No source provided");
            return;
        }

        if (settings.DebugMode)
            AdapterUtils.WaitForDebugger(mode, logger);

        if (!ableToRun(settings, logger))
            return;

        sources = AdapterUtils.GetSources(sources, settings, logger);
        if (sources is null)
        {
            return;
        }

        try
        {
            var discovery = this.shim.CreateDiscoverer();
            var deployment = this.shim.GetDeployment(settings,
                                                 logger,
                                                 loggerFactory,
                                                 out var registry,
                                                 cancellationTokenSource.Token);

            var vsTestCases = discovery.DiscoverTests(sources: sources,
                                                  settings: settings,
                                                  deployment: deployment,
                                                  log: logger,
                                                  cancellationToken: cancellationTokenSource.Token);
            vsTestCases = this.filterTests(vsTestCases, runContext, logger);
            if (vsTestCases.Count == 0)
            {
                return;
            }
            RunTests(vsTestCases,
                     settings,
                     frameworkHandle,
                     deployment,
                     registry,
                     logger);
        }
        finally
        {
            settings?.ExecuteCleanups(logger);
        }
    }

    public bool ShouldAttachToTestHost(IEnumerable<string>? sources,
                                       IRunContext runContext)
        => false;

    public bool ShouldAttachToTestHost(IEnumerable<TestCase>? tests,
                                       IRunContext runContext)
        => false;
}