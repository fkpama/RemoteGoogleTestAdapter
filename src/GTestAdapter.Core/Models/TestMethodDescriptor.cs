using GoogleTestAdapter.Model;
using GoogleTestAdapter.Remote.Models;
using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core.Models
{
    public class TestMethodDescriptor
    {
        public ElfDebugBinary File { get; }
        public string MethodName { get; }
        public string Suite { get; }
        public int ConnectionId { get; }
        public string? RemoteExePath { get; }
        public string SourceFile { get; }
        public TestCaseDescriptor TestCase { get; }
        public int TotalTestsInSuite { get; }
        public int TotalTestInExe { get; }

        public TestMethodDescriptor(ElfDebugBinary file,
                                    TestCaseDescriptor testCase,
                                    string methodName,
                                    string suite,
                                    string sourceFile,
                                    int connectionId,
                                    string? remoteExePath,
                                    int totalTestsInSuite,
                                    int totalTestInExe)
        {
            this.File = Guard.NotNull(file);
            this.TestCase = testCase;
            this.TotalTestsInSuite = totalTestsInSuite;
            this.TotalTestInExe = totalTestInExe;
            this.MethodName = methodName;
            this.Suite = suite;
            this.ConnectionId = connectionId;
            this.RemoteExePath = remoteExePath;
            this.SourceFile = Guard.NotNullOrWhitespace(sourceFile);
        }
    }
}
