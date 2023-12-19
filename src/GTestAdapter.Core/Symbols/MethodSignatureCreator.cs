using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Remote.Symbols
{
    internal class MethodSignatureCreator
    {

        internal IEnumerable<string> GetTestMethodSignatures(TestCaseDescriptor descriptor)
            => descriptor.TestType switch
        {
            TestCaseDescriptor.TestTypes.TypeParameterized => GetTypedTestMethodSignatures(descriptor),
            TestCaseDescriptor.TestTypes.Parameterized => GetParameterizedTestMethodSignature(descriptor).Yield(),
            TestCaseDescriptor.TestTypes.Simple => GetTestMethodSignature(descriptor.Suite, descriptor.Name).Yield(),
            _ => throw new UnreachableException(),
        };

        private IEnumerable<string> GetTypedTestMethodSignatures(TestCaseDescriptor descriptor)
        {
            var result = new List<string>();

            // remove instance number
            var suite = descriptor.Suite.Substring(0, descriptor.Suite.LastIndexOf("/", StringComparison.Ordinal));

            // remove prefix
            if (suite.Contains("/"))
            {
                var index = suite.IndexOf("/", StringComparison.Ordinal);
                suite = suite.Substring(index + 1, suite.Length - index - 1);
            }

            var typeParam = "<.+>";

            // <testcase name>_<test name>_Test<type param value>::TestBody
            result.Add(GetTestMethodSignature(suite, descriptor.Name, typeParam));

            // gtest_case_<testcase name>_::<test name><type param value>::TestBody
            var signature =
                $"gtest_case_{suite}_::{descriptor.Name}{typeParam}{GoogleTestConstants.TestBodySignature}";
            result.Add(signature);

            return result;
        }

        private string GetParameterizedTestMethodSignature(TestCaseDescriptor descriptor)
        {
            // remove instance number
            var index = descriptor.Suite.IndexOf('/');
            var suite = index < 0 ? descriptor.Suite : descriptor.Suite.Substring(index + 1);

            index = descriptor.Name.IndexOf('/');
            var testName = index < 0 ? descriptor.Name : descriptor.Name.Substring(0, index);

            return GetTestMethodSignature(suite, testName);
        }

        private string GetTestMethodSignature(string suite, string testCase, string typeParam = "")
        {
            return suite + "_" + testCase + "_Test" + typeParam + GoogleTestConstants.TestBodySignature;
        }

    }
}
