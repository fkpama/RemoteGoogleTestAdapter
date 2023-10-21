using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleTestAdapter;
using GoogleTestAdapter.Helpers;

namespace GTestAdapter.Core.Binary
{
    internal class MethodSignatureCreator
    {

        internal IEnumerable<string> GetTestMethodSignatures(TestCaseDescriptor descriptor)
        {
            switch (descriptor.TestType)
            {
                case TestCaseDescriptor.TestTypes.TypeParameterized:
                    return GetTypedTestMethodSignatures(descriptor);
                case TestCaseDescriptor.TestTypes.Parameterized:
                    return GetParameterizedTestMethodSignature(descriptor).Yield();
                case TestCaseDescriptor.TestTypes.Simple:
                    return GetTestMethodSignature(descriptor.Suite, descriptor.Name).Yield();
                default:
                    throw new UnreachableException();
            }
        }

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
