using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GTestAdapter.Core.Binary;
using GTestAdapter.Core.Models;
using GTestAdapter.Core.Settings;
using Microsoft.Extensions.Logging;
using Sodiware;
using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core
{
    public sealed class TestDiscoveryContext
    {
        private readonly ElfDebugBinary binary;
        private readonly string source;
        private readonly AdapterSettings settings;
        private readonly ILogger<TestDiscoveryContext> log;
        private readonly ITestFrameworkReporter testCaseReporter;

        public TestDiscoveryContext(ElfDebugBinary binary,
                                    string source,
                                    AdapterSettings settings,
                                    ITestFrameworkReporter testCaseReporter,
                                    ILogger<TestDiscoveryContext> log)
        {
            this.binary = binary;
            this.source = source;
            this.settings = settings;
            this.testCaseReporter = testCaseReporter;
            this.log = log.Safe();
        }

        public async Task DiscoverTests(TestProvider provider,
                                        ITestLocationResolver? locationResolver,
                                        CancellationToken cancellationToken)
        {
            log.StartingDiscovery(this.source);

            Task<IList<TestCaseDescriptor>> descriptorsTask;

            provider.OnTestMethod += (o, e) => onTestMethod(e.Descriptor,
                                                            locationResolver,
                                                            cancellationToken);
            descriptorsTask = provider.GetTestCasesAsync(cancellationToken);

            await descriptorsTask.ConfigureAwait(false);
        }

        private void onTestMethod(TestMethodDescriptor descriptor,
                                  ITestLocationResolver? resolver,
                                  CancellationToken cancellationToken)
        {
            var lineNumber = 0u;
            var testNamespace = string.Empty;
            var name = descriptor.MethodName;
            var fqname = $"{descriptor.Suite}.{name}";
            string? codeFilePath = null;
            log.LogDebug("Found test method {fqname}", fqname);
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
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error resolving location for test {fqname}: {msg}", ex.Message);
                }

            }

            var testCase = new TestCase(fqname,
                                        fqname,
                                        descriptor.SourceFile,
                                        fqname,
                                        codeFilePath,
                                        (int)lineNumber);
            this.testCaseReporter.ReportTestsFound(new[] { testCase });
        }
    }
}
