using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using Moq;
using Sodiware.Unix.DebugLibrary;
using Moq.Language.Flow;

namespace GTestAdapter.Core.Tests
{
    internal static class Extensions
    {
        internal static ISetup<ITestFrameworkReporter> ExpectSingleTestCase(this Mock<ITestFrameworkReporter> reporter, string name)
            => reporter.Setup(x => x.ReportTestsFound(It.Is<IEnumerable<TestCase>>(x => x.Count() == 1 && string.Equals(x.First().FullyQualifiedName, name))));
        internal static IReturnsResult<ISourceDeployment> HasTestListOutput(this Mock<ISourceDeployment> reporter, string[] output)
            => reporter.Setup(x => x.GetTestListOutputAsync(It.IsAny<string>(), It.IsAny<ElfDebugBinary>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(output);
    }
}
