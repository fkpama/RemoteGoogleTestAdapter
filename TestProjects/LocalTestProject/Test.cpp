#include <gtest/gtest.h>

TEST(LocalTestSuite, suite_example_test1)
{
  ASSERT_EQ(0, 0);	
}

TEST(LocalTestSuite, suite_example_test2)
{
  ASSERT_EQ(0, 0);
}


TEST(LocalTestSuite, failing_test1)
{
  int expected = 1;
  int actual = 0;
  ASSERT_EQ(expected, actual);
}
