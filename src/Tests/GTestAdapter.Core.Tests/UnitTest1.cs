using DebugLibrary.Tests.Utils;
using Microsoft.Extensions.Logging;
using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core.Tests
{
    public abstract class GtestCompiler
    {
        private readonly WslLinuxRunner compiler;
        protected ILoggerFactory loggerFactory;
        private readonly List<string> filesToRemove = new();

        public GtestCompiler()
        {
            this.compiler = new WslLinuxRunner();
        }
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
            var failed = TestContext.CurrentContext.Result.Outcome.Status != NUnit.Framework.Interfaces.TestStatus.Failed;
            if (!failed)
            {
                foreach (var file in this.filesToRemove)
                {
                    Utils.SafeDelete(file);
                }
            }
        }
    }
    public class Tests : GtestCompiler
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task can_find_tests_with_TEST_F_in_namespace()
        {
            const string sources = @"#include <gtest/gtest.h>
namespace TestNamespace
{
class MyTest : public ::testing::Test
{
};

TEST_F(MyTest, some_test)
{
ASSERT_EQ(0, 0);
}
}
";
            var (output, filename) = await this.GetTests(sources);

            var bin = new ElfDebugBinary(filename);
            var result = GTestListParser.Parse(output, bin);
            result.Count.Should().Be(1);
        }

        [Test]
        public async Task can_find_tests_with_TEST_F()
        {
            const string sources = @"#include <gtest/gtest.h>
class MyTest : public ::testing::Test
{
};

TEST_F(MyTest, some_test)
{
ASSERT_EQ(0, 0);
}
";
            var (output, filename) = await this.GetTests(sources);

            var bin = new ElfDebugBinary(filename);
            var result = GTestListParser.Parse(output, bin);
            result.Count.Should().Be(1);
        }

        [Test]
        public async Task can_find_tests()
        {
            const string sources = @"#include <gtest/gtest.h>
TEST(MyTest, some_test)
{
ASSERT_EQ(0, 0);
}
";
            var (output, filename) = await this.GetTests(sources);

            var bin = new ElfDebugBinary(filename);
            var result = GTestListParser.Parse(output, bin);
            result.Count.Should().Be(1);
        }
    }
}