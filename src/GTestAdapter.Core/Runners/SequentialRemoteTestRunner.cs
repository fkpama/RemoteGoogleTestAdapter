using System.Text;
using GoogleTestAdapter.Remote.Debugger;
using GoogleTestAdapter.Remote.Execution;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Settings;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.TestResults;
using Sodiware.IO;

namespace GoogleTestAdapter.Remote.Runners
{
    public class SequentialRemoteTestRunner : ITestRunner
    {
        private readonly ITestFrameworkReporter reporter;
        private readonly AdapterSettings settings;
        private readonly ISourceDeployment deployment;
        private readonly IProcessExecutorProvider processExecutorProvider;
        private readonly ITestOutputFilter[] outputFilters;
        private readonly ILogger log;
        private readonly CancellationTokenSource cancellation = new();

        public SequentialRemoteTestRunner(ITestFrameworkReporter reporter,
                                          AdapterSettings settings,
                                          ISourceDeployment deployment,
                                          IProcessExecutorProvider processExecutorProvider,
                                          IEnumerable<ITestOutputFilter> outputFilters,
                                          ILogger log)
        {
            this.reporter = reporter;
            this.settings = settings;
            this.deployment = deployment;
            this.processExecutorProvider = processExecutorProvider;
            this.outputFilters = outputFilters.ToArray();
            this.log = log;
        }

        public void Cancel()
        {
            this.cancellation.Cancel();
        }

        void ITestRunner.RunTests(IEnumerable<TestCase> testCasesToRun,
                                  string baseDir,
                                  string workingDir,
                                  string userParameters,
                                  bool isBeingDebugged,
                                  IDebuggedProcessLauncher debuggedLauncher,
                                  IProcessExecutor executor)
        {
            Assumes.Null(baseDir);
            Assumes.Null(debuggedLauncher);
            Assumes.Null(executor);
            //Assumes.False(isBeingDebugged);
            //if (isBeingDebugged)
            //{
            //    throw new NotImplementedException();
            //}
            CancellationToken cancellationToken = this.cancellation.Token;
            this.RunTestsAsync(testCasesToRun,
                               workingDir,
                               userParameters,
                               cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        public async Task RunTestsAsync(IEnumerable<TestCase> testCasesToRun,
                                        string workingDir,
                                        string userParameters,
                                        CancellationToken cancellationToken)
        {
            foreach(var (exe, testCases) in testCasesToRun.GroupByExecutable())
            {
                var generator = new CommandLineGenerator(testCases,
                                                     exe.Length,
                                                     userParameters ?? string.Empty,
                                                     this.settings.GetWrapper());

                Debug.Assert(testCases.Count > 0);
                var testCase = testCases[0];
                string? remoteExe = await this.deployment
                            .GetRemoteOutputAsync(testCase, cancellationToken)
                            .ConfigureAwait(false);
                foreach (var args in generator.GetCommandLines())
                {
                    log.DebugInfo($"Starting test: {args.CommandLine}");
                    if (remoteExe.IsMissing())
                    {
                        if (remoteExe.IsMissing())
                        {
                            // TODO:
                            throw new NotImplementedException();
                        }
                    }
                    Assumes.NotNullOrEmpty(remoteExe);
                    var executor = await this
                            .processExecutorProvider
                            .GetExecutorAsync(testCase, cancellationToken)
                            .ConfigureAwait(false);

                    await this.doRun(testCases,
                                     remoteExe,
                                     workingDir,
                                     args.CommandLine,
                                     executor,
                                     cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private Task<int> doRun(IEnumerable<TestCase> testCases,
                                string executable,
                                string workingDir,
                                string userParameters,
                                IProcessExecutor executor,
                                CancellationToken cancellationToken)
        {
            var envVars = new Dictionary<string, string>();
            int retCode = -1;
            var parser  = new StreamingStandardOutputTestResultParser(testCases,
                                                                      log,
                                                                      reporter);
            List<string>? sb = settings.DebugMode ? new() : null;
            try
            {
                retCode = executor.ExecuteCommandBlocking(executable,
                                                          userParameters ?? string.Empty,
                                                          workingDir,
                                                          envVars,
                                                          null,
                                                          line =>
                                                          {
                                                              line = transformLine(line);
                                                              sb?.Add(line);
                                                              cancellation.Token.ThrowIfCancellationRequested();
                                                              parser.ReportLine(line);
                                                          });
                cancellationToken.ThrowIfCancellationRequested();
                parser.Flush();
                if (retCode == 127)
                {
                    // executable was missing. This is an
                    // error in the test framework
                    var error = GUtils.SanitizeBashError(sb);
                    var results = testCases.Select(x => new TestResult(x)
                    {
                        ErrorMessage = error,
                        Outcome = TestOutcome.Failed
                    }).ToList();
                    reporter.ReportTestResults(results);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to execute command: {ex.Message}");
                string msg = $" *** Error during test execution: {ex.Message}";
                var toCreate = testCases.Except(parser.TestResults.Select(x => x.TestCase));
                foreach (var result in toCreate.Select(x => new TestResult(x)))
                {
                    if (result.ErrorMessage.IsPresent())
                    {
                        msg += $" ***\n\n{result.ErrorMessage}";
                    }
                    result.ErrorMessage = msg;
                    result.Outcome = TestOutcome.Failed;
                    result.ErrorStackTrace = ex.StackTrace;
                }
            }
            finally
            {
                log.DebugInfo($"Command exit code: {retCode}");
                if (sb?.Count > 0)
                {
                    log.DebugInfo($"Test output:\n\n{string.Join("\n", sb)}");
                }
            }

            cancellation.Token.ThrowIfCancellationRequested();
            return retCode switch
            {
                0 => TaskResults.Zero,
                1 => TaskResults.One,
                _ => Task.FromResult(retCode)
            };
        }

        private string transformLine(string line)
        {
            if (this.outputFilters.Length == 0)
                return line;
            foreach(var filter in this.outputFilters)
            {
                try
                {
                    line = filter.Transform(line);
                }
                catch (Exception ex)
                {
                    log.DebugError($"Error transforming line: {ex.Message}.\nLine:{line}\n{ex}");
                }
            }
            return line;
        }
    }
}
