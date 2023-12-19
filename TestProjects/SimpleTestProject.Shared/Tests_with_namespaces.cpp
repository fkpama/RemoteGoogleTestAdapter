#include <gtest/gtest.h>
namespace ExampleNamespace {
namespace NestedExampleNamespace {
TEST(NestedNamespaceExampleSuite, nested_suite_example1) {}

class NestedNamespaceExampleFixture : public ::testing::Test {};

TEST_F(NestedNamespaceExampleFixture, nested_fixture_example1) {}
TEST_F(NestedNamespaceExampleFixture, nested_fixture_example2) {}

}  // namespace NestedExampleNamespace
}  // namespace ExampleNamespace

