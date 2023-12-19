using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using Sodiware.Unix.DebugLibrary;
using Moq.Language.Flow;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Models;
using DebugLibrary.Tests.Utils;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
using Sodiware;

namespace GTestAdapter.Core.Tests
{
    internal static class Extensions
    {
        internal static ISetup<ITestFrameworkReporter> ExpectSingleTestCase(this Mock<ITestFrameworkReporter> reporter, string name)
        {
            var setup = reporter
                .Setup(x => x.ReportTestsFound(It.Is<IEnumerable<TestCase>>(x => x.Count() == 1 && string.Equals(x.First().FullyQualifiedName, name))));
            setup.Verifiable();
            return setup;
        }
        class Visitor : ExpressionVisitor
        {
            private readonly Expression replacement;

            public Visitor(Expression replacement)
            {
                this.replacement = replacement;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Type == typeof(TestCase))
                    return replacement;
                return node;
            }
        }

        internal static ISetup<ITestFrameworkReporter> ExpectSingleTestCase(this Mock<ITestFrameworkReporter> reporter,
            Expression<Func<TestCase, bool>> expression)
        {
            // basic preparations
            var method = typeof(ITestFrameworkReporter).GetInstanceMethod(nameof(ITestFrameworkReporter.ReportTestsFound));
            var testCaseParam = Parameter(typeof(IEnumerable<TestCase>), "y");

            // predicate to check Count == 1
            var countPredicate = Equal(Call(typeof(Enumerable),
                                      nameof(Enumerable.Count),
                                      new[] { typeof(TestCase) },
                                      testCaseParam),
                                      Constant(1));

            // IEnumerable.First()
            var firstExpr = Call(typeof(Enumerable),
                nameof(Enumerable.First),
                new[]{typeof(TestCase) },
                testCaseParam);

            // Replace all the references of TestCase in the expression
            // by the call to IEnumerable<TestCase>.First
            var converted = new Visitor(firstExpr).Visit(expression.Body);


            // creates the It.Is<> call
            var predicate = And(countPredicate, converted);
            var body = Lambda(predicate, testCaseParam);
            var itCall = Call(typeof(It),
                              nameof(It.Is),
                              new[] { typeof(IEnumerable<TestCase>) },
                              body);


            // Create the Setup lambda
            var lambdaParam = Parameter(typeof(ITestFrameworkReporter), "x");
            var expr = Call(lambdaParam, method, itCall);
            var lambda = Lambda(expr, lambdaParam);

            var setup = reporter.Setup((Expression<Action<ITestFrameworkReporter>>)lambda);
            setup.Verifiable();
            return setup;
        }
        internal static IReturnsResult<ISourceDeployment> HasTestListOutput(this Mock<ISourceDeployment> reporter, string output)
            => HasTestListOutput(reporter, output.Split('\n'));
        internal static IReturnsResult<ISourceDeployment> HasTestListOutput(this Mock<ISourceDeployment> reporter, string[] output)
            => reporter.Setup(x => x.GetTestListOutputAsync(It.IsAny<string>(), It.IsAny<ElfDebugBinary>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestListResult(0, "dummy", output));
        internal static IReturnsResult<ISourceDeployment> HasTestBinary(
            this Mock<ISourceDeployment> reporter,
            string path,
            ElfDebugBinary binary)
            => reporter.Setup(x => x.IsGoogleTestBinary(path, out binary!))
            .Returns(true);
        internal static IReturnsResult<ISourceDeployment> HasTestBinary(
            this Mock<ISourceDeployment> reporter,
            CompilationResult result,
            string output)
        {
            reporter.HasTestListOutput(output);
            return reporter.HasTestBinary(result);
        }
        internal static IReturnsResult<ISourceDeployment> HasTestBinary(
            this Mock<ISourceDeployment> reporter,
            CompilationResult result)
        {
            ElfDebugBinary binary = new ElfDebugBinary(result.OutputFilename);
            return reporter.Setup(x => x.IsGoogleTestBinary(result.OutputFilename,
                                                     out binary!))
            .Returns(true);
        }
    }
}
