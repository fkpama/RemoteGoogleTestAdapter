using DebugLibrary.Tests.Utils;
using Microsoft.Extensions.Logging;

namespace GTestAdapter.Core.Tests
{
    public abstract class GtestCompiler
    {
        private readonly WslLinuxRunner compiler;
        protected ILoggerFactory loggerFactory;
        private readonly List<string> filesToRemove = new();
        protected CancellationToken cancellationToken;

        public GtestCompiler()
        {
            this.compiler = new WslLinuxRunner();
            this.loggerFactory = null!;
        }
        protected const string DefaultTestOutput = @"ExampleTest.
  some_example
";
        protected Task<CompilationResult> CompileTestAsync(CancellationToken cancellationToken = default)
            => CompileTestAsync(@"#include <gtest/gtest.h>
TEST(ExampleTest, some_example)
{
ASSERT_EQ(0, 0);
}
", cancellationToken);
        protected async Task<CompilationResult> CompileTestAsync(string sourceCode, CancellationToken cancellationToken = default)
        {
            var unit = new CompilationUnitRequest
            {
                Content = sourceCode,
                Name = TestContext.CurrentContext.Test.MethodName!,
                Language = Language.Cxx11
            };
            var result = await this.compiler.CompileAsync(
                units: new[] { unit },
                outputType: OutputType.Application,
                additionalLibraryDependencies: new[]{  "gtest_main", "gtest", "pthread" },
                cancellationToken: cancellationToken);

            result.ThrowIfFailed();

            filesToRemove.AddRange(result.Units.Select(x => x.ObjectFilePath));
            filesToRemove.AddRange(result.Units.Select(x => x.SourcePath));
            return result;
        }
        protected async Task<(string TestListOutput, string TestExe)> GetTests(string sourceCode, CancellationToken cancellationToken = default)
        {
            var result = await this.CompileTestAsync(sourceCode, cancellationToken);
            result.ThrowIfFailed();

            var testExe = result.OutputFilename;
            if (!testExe.StartsWith("/")
                && !testExe.StartsWith("."))
            {
                testExe = $"./{result.OutputFilename}";
            }

            var testListOutput = this.compiler.RunCommand(testExe, "--gtest_list_tests");
            testListOutput = Utils.RemoveRunningMain(testListOutput);
            Console.WriteLine($"Test list output:\n{testListOutput}\n\n");
            return (testListOutput, testExe);
        }

        [TearDown]
        public void BaseTestCleanup()
        {
            loggerFactory?.Dispose();
            var failed = TestContext.CurrentContext.Result.Outcome.Status != NUnit.Framework.Interfaces.TestStatus.Failed;
            if (!failed)
            {
                foreach (var file in this.filesToRemove)
                {
                    Utils.SafeDelete(file);
                }
            }
        }

        [SetUp]
        public void BaseTestSetUp()
        {
            this.loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));
        }
    }
}