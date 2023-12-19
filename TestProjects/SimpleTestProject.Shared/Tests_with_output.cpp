#include <gtest/gtest.h>


TEST(SuiteOfTestWithOutput, failing_test_with_stdout)
{
  int expected = 1;
  int actual = 0;
  fputs("Hello world\n", stdout);
  fflush(stdout);
  ASSERT_EQ(expected, actual);
}

TEST(SuiteOfTestWithOutput, failing_test_with_stderr)
{
  int expected = 1;
  int actual = 0;
  fputs("Hello world\n", stderr);
  fflush(stderr);
  ASSERT_EQ(expected, actual);
}

TEST(SuiteOfTestWithOutput, succeeding_test_with_stdout)
{
  fputs("Hello world\n", stdout);
  fflush(stdout);
}

TEST(SuiteOfTestWithOutput, succeeding_test_with_stderr)
{
  fputs("Hello world\n", stderr);
  fflush(stderr);
}
