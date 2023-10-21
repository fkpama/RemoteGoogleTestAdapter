using GoogleTestAdapter.Model;
using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core.Models
{
    public class TestMethodDescriptor
    {
        public ElfDebugBinary File { get; }
        public string MethodName { get; }
        public string Suite { get; }
        public string SourceFile { get; }
        public TestCaseDescriptor TestCase { get; }

        public TestMethodDescriptor(ElfDebugBinary file,
                                    TestCaseDescriptor testCase,
                                    string methodName,
                                    string suite,
                                    string sourceFile)
        {
            this.File = Guard.NotNull(file);
            this.TestCase = testCase;
            this.MethodName = methodName;
            this.Suite = suite;
            this.SourceFile = Guard.NotNullOrWhitespace(sourceFile);
        }
    }
}
