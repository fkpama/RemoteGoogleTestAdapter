using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core
{
    public interface ISourceDeployment
    {
        Task<string[]> GetTestListOutputAsync(string filePath,
                                              ElfDebugBinary binary,
                                              CancellationToken cancellationToken);
        bool IsGoogleTestBinary(string source,
                                [NotNullWhen(true)] out ElfDebugBinary? binary);
        Task<string?> MapRemoteFileAsync(string deploymentFile,
                                         string fullpath,
                                         CancellationToken cancellation);
    }
}
