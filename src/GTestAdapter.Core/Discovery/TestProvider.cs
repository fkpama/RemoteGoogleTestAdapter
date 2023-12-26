using Sodiware.Unix.DebugLibrary;
using GTestAdapter.Core.Models;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.TestCases;

namespace GoogleTestAdapter.Remote
{
    public sealed class TestProvider
    {
        private readonly ISourceDeployment deployment;
        private readonly ElfDebugBinary binary;
        private readonly string filePath;
        private readonly ILogger log;
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
                            ILogger log)
        {
            this.deployment = deployment;
            this.binary = binary;
            this.filePath = filePath;
            this.log = log;
            this.testNameSeparator = SettingsWrapper.OptionTestNameSeparatorDefaultValue;
        }

        public async Task<IList<TestCaseDescriptor>> GetTestCasesAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.deployment
                .GetTestListOutputAsync(this.filePath,
                                        this.binary,
                                        cancellationToken).NoAwait();
            var outputs = result.Outputs;
            var t1 = Task.Run(() => new ListTestsParser(this.testNameSeparator)
                .ParseListTestsOutput(outputs));
            var t2 = Task.Run(() => new ListTestMetadataParser()
                .ParseTestListOutput(outputs));
            await Task.WhenAll(t1, t2).NoAwait();
            var testCases = t1.Result;
            var metadatas = t2.Result;
            var flags = result.Flags;

            foreach (var testCase in testCases)
            {
                var suite = testCase.Suite;
                var name = testCase.Name;
                //log.LogDebug("TestCase found: {name}", testCase.FullyQualifiedName);

                var nbTestInSuite = metadatas.NbTestPerSuite[suite];
                var tcase = new TestMethodDescriptor(this.binary,
                                                     testCase,
                                                     name,
                                                     suite,
                                                     this.filePath,
                                                     result.ConnectionId,
                                                     result.RemotePath,
                                                     nbTestInSuite,
                                                     metadatas.TotalTestInExe,
                                                     flags);
                this.OnTestMethod?.Invoke(this, new(tcase));
            }
            return testCases;
        }

    }
}
