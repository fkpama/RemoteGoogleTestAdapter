using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Symbols;
using GoogleTestAdapter.TestCases;
using GTestAdapter.Core.Models;
using Microsoft.Extensions.Logging;
using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core.Tests
{
    internal class TestLocationResolverTests : GtestCompiler
    {
        private TestLocationResolver sut;
        Mock<ISourceDeployment> mockDeployment;

        [SetUp]
        public void Setup()
        {
            mockDeployment = new();
            sut = new(mockDeployment.Object, loggerFactory.CreateLogger<TestLocationResolver>());
        }

        [Test]
        public async Task Can_find_symbol()
        {

            const string src = @"#include <gtest/gtest.h>

TEST(Example, method)
{
};
";

            string[] output = @"
Example.
  method
".Trim().Split('\n');

            var result = await this.CompileTestAsync(src, cancellationToken);
            var bin = new ElfDebugBinary(result.OutputFilename);
            //mockDeployment.HasTestListOutput(output).Verifiable();
            //mockReporter.ExpectSingleTestCase("Example.method").Verifiable();
            var tcDesc = new ListTestsParser("").ParseListTestsOutput(output);
            var descriptor = new TestMethodDescriptor(bin,
                                                      tcDesc[0],
                                                      "method",
                                                      "Example",
                                                      result.OutputFilename,
                                                      0,
                                                      null,
                                                      1,
                                                      1);
            var resolved = await sut.ResolveAsync(descriptor, cancellationToken);

        }
        [Test]
        public async Task Can_find_symbol_with_namespace()
        {

            const string src = @"#include <gtest/gtest.h>

namespace NS1 {
TEST(Example, method)
{
}
};
";

            string[] output = @"
Example.
  method
".Trim().Split('\n');

            var result = await this.CompileTestAsync(src, cancellationToken);
            var bin = new ElfDebugBinary(result.OutputFilename);
            //mockDeployment.HasTestListOutput(output).Verifiable();
            //mockReporter.ExpectSingleTestCase("Example.method").Verifiable();
            var tcDesc = new ListTestsParser("").ParseListTestsOutput(output);
            var descriptor = new TestMethodDescriptor(bin,
                                                      tcDesc[0],
                                                      "method",
                                                      "Example",
                                                      result.OutputFilename,
                                                      0,
                                                      null,
                                                      1,
                                                      1);
            var resolved = await sut.ResolveAsync(descriptor, cancellationToken);

        }
     }
}
