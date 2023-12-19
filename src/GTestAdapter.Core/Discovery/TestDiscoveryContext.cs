using GoogleTestAdapter.Remote.Models;
using GoogleTestAdapter.Remote.Symbols;
using GTestAdapter.Core.Models;

namespace GoogleTestAdapter.Remote.Discovery
{
    public sealed class TestDiscoveryContext
    {
        private readonly string source;
        private readonly ILogger log;
        private readonly ITestFrameworkReporter testCaseReporter;

        public TestDiscoveryContext(string source,
                                    ITestFrameworkReporter testCaseReporter,
                                    ILogger log)
        {
            this.source = source;
            this.testCaseReporter = testCaseReporter;
            this.log = log;
        }

        public async Task DiscoverTests(TestProvider provider,
                                        ITestLocationResolver? locationResolver,
                                        CancellationToken cancellationToken)
        {
            //log.TestDiscoveryStarting(this.source);
            log.LogInfo($"Starting test discovery: {this.source}");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                Task<IList<TestCaseDescriptor>> descriptorsTask;

                provider.OnTestMethod += (o, e) => onTestMethod(e.Descriptor,
                                                                locationResolver,
                                                                cancellationToken);
                descriptorsTask = provider.GetTestCasesAsync(cancellationToken);

                await descriptorsTask.ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();
                //log.TestDiscoveryCompleted(this.source, stopwatch.Elapsed);
                log.LogInfo($"Test discovery for '{this.source}' completed, overall duration: {stopwatch.Elapsed}");
            }
        }

        private void onTestMethod(TestMethodDescriptor descriptor,
                                  ITestLocationResolver? resolver,
                                  CancellationToken cancellationToken)
        {
            var lineNumber = 0u;
            var name = descriptor.MethodName;
            var fqname = $"{descriptor.Suite}.{name}";
            TestCaseHierarchyProperty? hierarchy = null;
            string? codeFilePath = null;
            if (resolver is not null)
            {
                try
                {
                    var location = resolver
                    .ResolveAsync(descriptor, cancellationToken)
                    .GetAwaiter()
                    .GetResult();
                    if (location is not null)
                    {
                        codeFilePath = location.Sourcefile;
                        lineNumber = location.Line;

                        if (location.Namespaces?.Length > 0)
                        {
                            var str = string.Join(".", location.Namespaces);
                            hierarchy = new TestCaseHierarchyProperty(str, descriptor.Suite, descriptor.MethodName);
                        }
                    }
                    else
                    {
                        log.LogWarning($"No source found for test {fqname}");
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error resolving location for test {fqname}: {msg}", ex.Message);
                }

            }

            //log.DebugInfo($"Found test method {fqname}{(codeFilePath.IsPresent() ? $" ({codeFilePath}:{lineNumber})" : null)}");
            log.DebugInfo($"Found test method {fqname}{$" ({codeFilePath}:{lineNumber})"}");
            var testCase = new TestCase(fqname,
                                        fqname,
                                        descriptor.SourceFile,
                                        fqname,
                                        codeFilePath,
                                        (int)lineNumber);
            var metadata = new TestCaseMetaDataProperty(descriptor.TotalTestsInSuite,
                                                        descriptor.TotalTestInExe,
                                                        testCase.FullyQualifiedName);
            var deploymentMetadata = new TestCaseDeploymentProperty(descriptor.ConnectionId,
                descriptor.RemoteExePath);
            testCase.Properties.Add(metadata);
            testCase.Properties.Add(deploymentMetadata);

            if (hierarchy is not null)
            {
                testCase.Properties.Add(hierarchy);
            }
            this.testCaseReporter.ReportTestsFound(new[] { testCase });
        }
    }
}
