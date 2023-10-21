using GoogleTestAdapter;
using Microsoft.Extensions.Logging;
using Sodiware.Unix.DebugLibrary;
using Sodiware;
using GoogleTestAdapter.Model;
using GTestAdapter.Core.Models;

namespace GTestAdapter.Core
{
    public sealed class TestProvider
    {
        private readonly ISourceDeployment deployment;
        private readonly ElfDebugBinary binary;
        private readonly string filePath;
        private readonly ILogger<TestProvider> log;
        private readonly string testNameSeparator;

        public sealed class TestMethodEventArgs
        {
            public TestMethodDescriptor Descriptor { get; }
            public TestMethodEventArgs(TestMethodDescriptor descriptor)
            {
                this.Descriptor = descriptor;
            }
        }

        public event EventHandler<TestMethodEventArgs>? OnTestMethod;

        public TestProvider(ISourceDeployment deployment,
                            ElfDebugBinary binary,
                            string filePath,
                            ILogger<TestProvider> log)
        {
            this.deployment = deployment;
            this.binary = binary;
            this.filePath = filePath;
            this.log = log;
            this.testNameSeparator = SettingsWrapper.OptionTestNameSeparatorDefaultValue;
        }

        public async Task<IList<TestCaseDescriptor>> GetTestCasesAsync(CancellationToken cancellationToken = default)
        {
            var outputs = await this.deployment
                .GetTestListOutputAsync(this.filePath,
                                        this.binary,
                                        cancellationToken);
            var parser = new ListTestsParser(this.testNameSeparator);
            var testCases = parser.ParseListTestsOutput(outputs);

            foreach(var testCase in testCases)
            {
                    var suite = testCase.Suite;
                    var name = testCase.Name;
                    log.LogDebug("TestCase found: {name}", testCase.FullyQualifiedName);

                    var tcase = new TestMethodDescriptor(this.binary,
                                                         testCase,
                                                         name,
                                                         suite,
                                                         this.filePath);
                    this.OnTestMethod?.Invoke(this, new(tcase));
            }
            return testCases;
        }

    }
}
