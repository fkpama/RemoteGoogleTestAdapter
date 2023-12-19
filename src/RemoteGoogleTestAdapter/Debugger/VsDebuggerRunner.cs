using System.Runtime.InteropServices;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Remote.Adapter.Debugger.ServiceModel;
using GoogleTestAdapter.Remote.Adapter.VisualStudio;
using GoogleTestAdapter.Remote.Debugger;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Settings;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.TestResults;
using Microsoft.VisualStudio;

namespace GoogleTestAdapter.Remote.Adapter.Debugger
{
    internal class VsDebuggerRunner : ITestRunner
    {
        private readonly VsIde vsIde;
        private readonly AdapterSettings settings;
        private readonly ITestFrameworkReporter reporter;
        private readonly ISourceDeployment deployment;
        private readonly ILogger log;
        private readonly CancellationTokenSource cancellation = new();

        public VsDebuggerRunner(VsIde vsIde,
                                AdapterSettings settings,
                                ITestFrameworkReporter reporter,
                                ISourceDeployment deployment,
                                ILogger logger,
                                CancellationToken cancellationToken)
        {
            this.vsIde = vsIde;
            this.settings = settings;
            this.reporter = reporter;
            this.deployment = deployment;
            this.log = logger;
            this.cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public void Cancel()
        {
            this.log?.DebugInfo("Runner cancelled.");
            this.cancellation.Cancel();
        }

        public void RunTests(IEnumerable<TestCase> testCases,
                             string baseDir,
                             string workingDir,
                             string userParameters,
                             bool isBeingDebugged,
                             IDebuggedProcessLauncher debuggedLauncher,
                             IProcessExecutor executor)
        {
            CancellationToken token = this.cancellation.Token;
            VsIde.JoinableTaskFactory.Run(async () =>
            {
                //try
                //{
                var collection = testCases as IReadOnlyCollection<TestCase>
                ?? testCases.ToList();
                    await RunTestsAsync(collection,
                                        baseDir,
                                        workingDir,
                                        userParameters,
                                        cancellation.Token)
                    .ConfigureAwait(false);
                //}
                //catch (Exception ex)
                //{
                //    log.LogError($"Error launching debugger. {ex.Message}.\n\tCommand: {exe} {args.CommandLine}");
                //}
            });
        }

        private Task RunTestsAsync(IReadOnlyCollection<TestCase> testCases,
                                   string baseDir,
                                   string workingDir,
                                   string userParameters,
                                   CancellationToken cancellationToken)
        {
            var serviceId = Guid.NewGuid();
            this.log.DebugInfo($"Debug service ID: {serviceId}");
            var parser = new StreamingStandardOutputTestResultParser(testCases,
                log,
                reporter);
            using var server = new DebugServer(this.reporter,
                                               parser,
                                               cancellationToken,
                                               this.log);
            using var service = this.createDebuggerService(serviceId, server);
            var batches = this.getBatches(testCases,
                                          userParameters,
                                          cancellationToken);

            var model = new DebuggerLaunchParameters(serviceId, batches)
            {
                BaseDir = baseDir,
                WorkingDir = workingDir
            };
            var text = model.Serialize();
            var timeout = TimeSpan.FromSeconds(30);
            var startTime = Stopwatch.GetTimestamp();
            int result;

            // TODO: specific tests
            //reporter.ReportTestsStarted(testCases);
            try
            {
                ;
                result = this.vsIde.LaunchCommand<int>(GrtaDebuggerCommands.CommandId,
                                                       text,
                                                       timeout.GetRemaining(startTime));
            }
            catch (TimeoutException)
            {
                result = VSConstants.RPC_E_TIMEOUT;
            }
            if (result != 0)
                log.LogError($"Failed to launch debugger: 0x{result:X8}");
            Marshal.ThrowExceptionForHR(result);
            log.DebugInfo("Debugger launch command succeeded. Waiting for Visual studio debugging session");


            bool connected = false;
            if(!(connected = server.ConnectedEvent.WaitOne(timeout.GetRemaining(startTime), cancellation.Token)))
            {
                var msg = $"Debugging session did not start in time {timeout}";
                reporter.ReportTestResults(testCases
                    .ToTestResult(TestOutcome.Skipped, msg));

            }
            else
            {
                log.LogInfo("Debug client successfully attached.");

                server.Wait(cancellation.Token);
                parser.Flush();
                log.LogInfo($"Debug session ended ({parser.TestResults.Count}/{testCases.Count} results)");
            }


            this.cleanup(server);

            return Task.CompletedTask;
        }

        private void cleanup(DebugServer server)
        {
            //if (server.RemainingTestCases.Count > 0)
            //{
            //    log.LogWarning($"{server.RemainingTestCases.Count} test(s) were not executed.");
            //    var results = server.RemainingTestCases
            //        .Select(x => new TestResult(x)
            //        {
            //            Outcome = TestOutcome.Skipped
            //        }).ToArray();
            //    this.reporter.ReportTestResults(results);
            //}
        }

        private List<TestLaunchBatch> getBatches(IReadOnlyCollection<TestCase> testCases,
                                          string userParameters,
                                          CancellationToken cancellationToken)
        {
            var lst = new List<TestLaunchBatch>();
            foreach (var (exe, cases) in testCases.GroupByExecutable())
            {
                Assumes.True(cases.Count > 0);
                var projectId = this.deployment.GetOutputProjectName(exe);
                var cmdLineBuild = new CommandLineGenerator(cases,
                                                            exe.Length,
                                                            userParameters.IfMissing(string.Empty),
                                                            this.settings.GetWrapper());
                foreach(var args in cmdLineBuild.GetCommandLines())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var batch = new TestLaunchBatch
                    {
                        CommandLine = args.CommandLine,
                        //ExecutablePath = exe,
                        ProjectId = projectId
                    };
                    lst.Add(batch);
                    //try
                    //{
                    //    this.reporter.ReportTestsStarted(args.TestCases);
                    //}
                    //catch (Exception ex)
                    //{
                    //}
                }
            }
            return lst;
        }

        private IDebuggerHost createDebuggerService(Guid guid, DebugServer server)
        {
            var factory = new DebuggerServiceFactory();
            var host = factory.Open(guid, server, log);
            return host;
        }
    }
}
