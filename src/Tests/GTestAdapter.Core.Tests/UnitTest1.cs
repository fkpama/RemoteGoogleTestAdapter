using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core.Tests
{
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
            var result = Utils.Parse(output); //GTestListParser.Parse(output, bin);
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
            //var result = GTestListParser.Parse(output, bin);
            //result.Count.Should().Be(1);
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
            //var result = GTestListParser.Parse(output, bin);
            //result.Count.Should().Be(1);
        }
    }
}