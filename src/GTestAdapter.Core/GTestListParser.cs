using System.Runtime.InteropServices;
using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core
{
    public class GTestListParser
    {
        public  static IList<LinuxTestCaseDescriptor> Parse(string output,
                                                       ElfDebugBinary binary)
        {
            var lines = output.Trim().Split('\n').ToList();

            if (lines[0].StartsWith("Running main() from ", StringComparison.Ordinal))
            {
                lines.RemoveAt(0);
            }

            var descriptors = ParseSanitized(lines);
            var tests = new List<LinuxTestCaseDescriptor>();
            foreach(var test in descriptors)
            {
                var symbol = $"{test.Suite}_{test.Name}_Test::TestBody()";
                var function = binary.GetMethod(x => x.Signature?.EndsWith(symbol, StringComparison.Ordinal) == true);
                tests.Add(new(test, function));
            }
            return tests;
        }

        private static IList<TestCaseDescriptor> ParseSanitized(List<string> lines)
        {
            var parser = new ListTestsParser(SettingsWrapper.OptionTestNameSeparatorDefaultValue);
            return parser.ParseListTestsOutput(lines);
        }
    }

    public class LinuxTestCaseDescriptor
    {
        public LinuxTestCaseDescriptor(TestCaseDescriptor descriptor, ClassMethodDebugInfo method)
        {
            this.Descriptor = descriptor;
            this.Method = method;
        }

        public TestCaseDescriptor Descriptor { get; }
        public ClassMethodDebugInfo Method { get; }
    }
}
