using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.TestResults;

namespace GoogleTestAdapter.Remote.Adapter.Debugger
{
    internal sealed class DebugServer : IDebuggerLauncher, IDisposable
    {
        private readonly ITestFrameworkReporter reporter;
        private readonly CancellationToken cancellationToken;
        private readonly ILogger log;
        private readonly ManualResetEvent testDoneEvent = new(false);
        private readonly ManualResetEventSlim connectedEvent = new(false);

        public bool Connected { get; private set; }

        public WaitHandle ConnectedEvent => this.connectedEvent.WaitHandle;

        public StreamingStandardOutputTestResultParser Parser { get; }

        public DebuggerShutdownReason? ExitReason { get; private set; }

        public DebugServer(ITestFrameworkReporter reporter,
                           StreamingStandardOutputTestResultParser parser,
                           CancellationToken cancellationToken,
                           ILogger log)
        {
            this.reporter = reporter;
            this.Parser = parser;
            this.cancellationToken = cancellationToken;
            this.log = log;
        }

        public void ReportTestResults(TestResultModel[] results)
        {
            //var lst = new List<TestResult>(results.Length);
            //foreach(var resultModel in results)
            //{
            //    var test = this.testCases.FirstOrDefault(x => string.Equals(x.FullyQualifiedNameWithNamespace, resultModel.FullyQualifiedNameWithNamespace, StringComparison.Ordinal));
            //    if (test is null)
            //    {
            //        log.LogWarning($"Unknown result test case {resultModel.FullyQualifiedNameWithNamespace}");
            //    }
            //    else
            //    {
            //        var result = new TestResult(test)
            //        {
            //            Outcome = resultModel.Outcome.ToTestOutcome()
            //            //Duration = resultModel.Duration.AsMilliseconds()
            //        };
            //        lst.Add(result);
            //    }
            //}

            //lock (this.testCases)
            //{

            //    lst.Select(x => x.TestCase).ForEach(x => this.testCases.Remove(x));
            //    (this.testResults ??= new(this.AllTestCases.Count)).AddRange(lst);
            //}
            //if (lst.Count > 0)
            //    this.reporter.ReportTestResults(lst);
        }

        public void ReportTestStarted(string[] tests)
        {
            log.LogInfo($"Tests started: {string.Join(", ", tests)}");
        }

        public void Shutdown(DebuggerShutdownReason reason)
        {
            this.ExitReason = reason;
            log.DebugInfo($"Test adapter debug proxy shutdown: {reason}");
            this.testDoneEvent.Set();
        }

        void IDebuggerLauncher.ClientConnected()
        {
            this.Connected = true;
            this.connectedEvent.Set();
        }

        void IDebuggerLauncher.OutputReceived(string outputText)
        {
            log.DebugInfo($"Output received: {outputText}");
            if (outputText.IsMissing())
                return;
            var lines = outputText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWithO("Running main() from"))
                    continue;
                else if (line.StartsWithO("Note: Google Test filter ="))
                    continue;
                log.DebugInfo($"Line: {line}");
                this.Parser.ReportLine(line);
            }
        }

        void IDebuggerLauncher.ErrorReceived(string errorText)
        {
            log.LogWarning($"Not supposed to received data on error channel");
            this.Parser.ReportLine(errorText);
        }

        internal void Wait(CancellationToken cancellationToken)
        {
            log.DebugInfo($"Starting server wait");
            this.testDoneEvent.WaitOne(cancellationToken);
        }

        void IDisposable.Dispose() { }
    }
}
