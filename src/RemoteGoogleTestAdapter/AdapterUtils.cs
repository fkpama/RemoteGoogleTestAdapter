using System.Reflection;
using GoogleTestAdapter.Remote.Adapter.Logging;
using GoogleTestAdapter.Remote.Adapter.Settings;
using GoogleTestAdapter.Remote.Adapter.VisualStudio;
using GoogleTestAdapter.Remote.Settings;
using GoogleTestAdapter.Runners;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Remote.Adapter
{
    internal static class AdapterUtils
    {
        static AdapterUtils()
        {
            initializeRuntime();
        }

        static void initializeRuntime()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (o, e) =>
            {
                var name = new AssemblyName(e.Name);
                var path = Path.GetDirectoryName(typeof(AdapterUtils).Assembly.Location)!;
                path = Path.Combine(path, $"{name.Name}.dll");
                if (File.Exists(path))
                {
                    return Assembly.LoadFile(path);
                }
                return null;
            };
        }

        internal static bool DebugMode
        {
            get => Environment.GetEnvironmentVariable(DebugEnv).IsPresent();
        }

        internal static readonly char[] LineSeparatorChars = new[]{'\r', '\n'};
        const string DebugEnv = "GTEST_WAIT_FOR_DEBUGGER",
            ExecutorDebugEnv = "GTEST_EXECUTOR_WAIT_FOR_DEBUGGER",
            DiscoveryDebugEnv = "GTEST_DISCOVERY_WAIT_FOR_DEBUGGER";
        [DebuggerStepThrough]
        internal static void WaitForDebugger(AdapterMode mode, ILogger logger)
        {
            var env2 = mode == AdapterMode.Execution ? ExecutorDebugEnv :  DiscoveryDebugEnv;
            string? val = null;
            if (DebugMode || (val = Environment.GetEnvironmentVariable(env2)).IsPresent())
            {
                string? envName = DebugMode ? DebugEnv : env2;
                logger.LogInfo($"Env: {envName}={val}");
                var countdown = new Stopwatch();
                while (!SysDebugger.IsAttached && countdown.Elapsed < TimeSpan.FromMinutes(1))
                {
                    logger.LogInfo($"Waiting for debugger. PID: {Process.GetCurrentProcess().Id}");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                if (SysDebugger.IsAttached)
                {
                    SysDebugger.Break();
                }
            }
        }
        internal static void Initialize(IDiscoveryContext? context,
                                        out AdapterSettings adapterSettings,
                                        IMessageLogger? messageLogger,
                                        out ILogger frameworkLogger,
                                        out ILoggerFactory loggerFactory)
        {
            VsTestFrameworkAdapterSettings settings = null!;
            var logger = new VsTestFrameworkLogger(messageLogger,
                () => settings!.DebugMode,
                () => settings!.TimestampOutput);
            frameworkLogger = logger;
            settings = new(context?.RunSettings, logger);
            adapterSettings = settings;
            if (context is IRunContext ctx)
            {
                //if (ctx.SolutionDirectory.IsPresent())
                //    settings.SetIsRunningInsideVisualStudio();

                if (ctx.IsBeingDebugged)
                {
                    settings.SetIsBeingDebugged();
                }
            }
            loggerFactory = new VsTestFrameworkLoggerFactory(messageLogger, settings);
        }

        internal static ISshClient GetSshClient(int connectionId, CancellationToken cancellationToken)
        {
            var client = ConnectionFactory.CreateClient(connectionId, VsIde.JoinableTaskFactory, cancellationToken);
            return client;
        }

        internal static ISshClient GetSshClient(CancellationToken cancellationToken)
        {
            var client = ConnectionFactory.CreateClient(VsIde.JoinableTaskFactory, cancellationToken);
            return client;
        }

        [return: NotNullIfNotNull(nameof(sources))]
        internal static IEnumerable<string> GetSources(IEnumerable<string> sources,
                                                        AdapterSettings settings,
                                                        ILogger logger)
            => GetSources(sources, settings, out _, logger);

        [return: NotNullIfNotNull(nameof(sources))]
        internal static IEnumerable<string> GetSources(IEnumerable<string> sources,
                                                        AdapterSettings settings,
                                                        out string? originalSource,
                                                        ILogger logger)
        {
            string? overrideSource = settings.OverrideSource;
            if (!string.IsNullOrWhiteSpace(overrideSource))
            {
                logger.LogInfo($"Overriding test source discovery: {settings.OverrideSource}");
                originalSource = sources.FirstOrDefault();
                Assumes.NotNullOrEmpty(overrideSource);
                sources = new[] { overrideSource };
            }
            else
            {
                originalSource = null;
            }
            return sources;
        }

        //internal static ICollection<TestCase> Consolidate(IEnumerable<VsTestCase> vsTestCases, ILogger logger)
        //{
        //    var allTests = vsTestCases.Select(x => x.ToTestCase()).ToList();

        //    int nbTestInExe = int.MinValue;
        //    foreach(var (exe, testCases) in allTests.GroupByExecutable())
        //    {
        //        var suiteDict = testCases.GroupBySuite().ToArray();
        //        var nbDict = new Dictionary<string, int>(suiteDict.Length);
        //        foreach(var (suite, suiteCases) in suiteDict)
        //        {
        //            var nbTestInSuite = suiteCases.Max(x =>
        //            {
        //                var meta = x.Properties.OfType<TestCaseMetaDataProperty>().FirstOrDefault();
        //                if (meta is null)
        //                {
        //                    logger.DebugWarning($"Test {x.FullyQualifiedName} does not have metadatas.");
        //                    return int.MaxValue;
        //                }
        //                nbTestInExe = Math.Max(meta.NrOfTestCasesInExecutable, nbTestInExe);
        //                return meta.NrOfTestCasesInSuite;
        //            });

        //            nbDict[suite] = nbTestInSuite;
        //        }

        //        foreach (var (suite, suiteCases) in suiteDict)
        //        {
        //            var nbTestInSuite = nbDict[suite];
        //            foreach(var testCase in suiteCases)
        //            {
        //                var meta = testCase.Properties.OfType<TestCaseMetaDataProperty>().FirstOrDefault();
        //                if (meta is not null)
        //                    testCase.Properties.Remove(meta);
        //                var name = testCase.FullyQualifiedNameWithNamespace;
        //                TestCaseMetaDataProperty consolidated = new(nbTestInSuite, nbTestInExe, name);
        //                testCase.Properties.Add(consolidated);
        //            }

        //        }
        //    }

        //    return allTests;
        //}

        internal static void RunTests(this ITestRunner testRunner,
                                      IEnumerable<VsTestCase> testCases,
                                      bool isBeingDebugged)
        {
            testRunner.RunTests(testCases.Select(x => x.ToTestCase()),
                                null,
                                null,
                                null,
                                isBeingDebugged,
                                null,
                                null);
        }
    }
}
