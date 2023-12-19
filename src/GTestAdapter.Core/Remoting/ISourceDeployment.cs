using GoogleTestAdapter.Remote.Models;
using Sodiware.Unix.DebugLibrary;

namespace GoogleTestAdapter.Remote.Remoting
{
    public interface ISourceDeployment
    {
        //Task DeployAsync(string source, CancellationToken token);
        string? GetOutputProjectName(string exe);
        Task<string?> GetRemoteOutputAsync(TestCase testCase, CancellationToken cancellationToken);
        Task<TestListResult> GetTestListOutputAsync(string filePath,
                                                    ElfDebugBinary binary,
                                                    CancellationToken cancellationToken);
        bool IsGoogleTestBinary(string source,
                                [NotNullWhen(true)] out ElfDebugBinary? binary);
        Task<string?> MapRemoteFileAsync(string deploymentFile,
                                         string fullpath,
                                         CancellationToken cancellation);
    }
}
