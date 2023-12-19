using GoogleTestAdapter.Remote.TestCases;

namespace GTestAdapter.Core.Tests
{
    internal class ListTestMetadataParserTests
    {
        private ListTestMetadataParser sut;

        public ListTestMetadataParserTests()
        {
            this.sut = null!;
        }

        [SetUp]
        public void SetUp()
        {
            this.sut = new();
        }

        [Test]
        public void can_parse_tests()
        {
            var output = TestDatas.TestOutput1;
            var result = this.sut.ParseTestListOutput(output.Output);
            Assert.That(output.TotalTests, Is.EqualTo(result.TotalTestInExe));
            foreach(var (suite, count) in output.NbTestsPerSuite)
            {
                Assert.That(result.NbTestPerSuite.ContainsKey(suite));
                var found = result.NbTestPerSuite[suite];
                Assert.That(found, Is.EqualTo(count));
            }
        }
    }
}
