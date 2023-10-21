#include <gtest/gtest.h>

TEST(ExampleTestSuite, suite_example_test1)
{
  ASSERT_EQ(0, 0);
}

TEST(ExampleU_TestSuite, suite_example_test2)
{
  ASSERT_EQ(0, 0);
}

namespace ExampleNamespace {
namespace NestedExampleNamespace {
TEST(NestedNamespaceExampleSuite, nested_suite_example1) {}

class NestedNamespaceExampleFixture : public ::testing::Test {};

TEST_F(NestedNamespaceExampleFixture, nested_fixture_example1) {}
TEST_F(NestedNamespaceExampleFixture, nested_fixture_example2) {}

}  // namespace NestedExampleNamespace
}  // namespace ExampleNamespace