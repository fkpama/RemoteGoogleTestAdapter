using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Remote.Adapter;
using GoogleTestAdapter.Remote.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;

namespace GTestAdapter.Core.Tests
{
    internal class TestExecutorTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test, Ignore("TODO")]
        public void can_run_tests()
        {
            var sut = new TestExecutor();
            //var testCase = new VsTestCase("x", "y", "z", "foo", "bar", 0);
            var uri = new Uri(TestExecutor.ExecutorUri);
            var testCase = new VsTestCase("SomeName.SomeTest", uri, "source");
            testCase.SetMetadataProperty(1, 1, "SomeName.SomeTest");
            testCase.SetDeploymentProperty(0, "/some/path", 0);
            var mockContext = new Mock<IRunContext>();
            var handle = new Mock<IFrameworkHandle>();
            sut.RunTests(new[] { testCase }, mockContext.Object,
                handle.Object);
        }

        [Test]
        public void can_debug_tests()
        {
            var sut = new TestExecutor();
            var uri = new Uri(TestExecutor.ExecutorUri);
            var testCase = new VsTestCase("Exe", uri, "source");
            var mockContext = new Mock<IRunContext>();
            var handle = new Mock<IFrameworkHandle>();
            sut.RunTests(new[] { testCase },
                         mockContext.Object,
                         handle.Object);
        }
    }
}
