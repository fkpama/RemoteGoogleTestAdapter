
using GoogleTestAdapter;
using liblinux.IO;
using Microsoft.Extensions.Logging;
using Sodiware.Unix.DebugLibrary;

namespace GTestAdapter.Core
{
    public class SourceDeployment : ISourceDeployment
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<SourceDeployment> log;
        private readonly ISshClient client;

        public SourceDeployment(ISshClient client,
                                ILoggerFactory logger)
        {
            this.loggerFactory = logger.Safe();
            this.log = this.loggerFactory.CreateLogger<SourceDeployment>();
            this.client = client;
        }

        public bool IsGoogleTestBinary(string source,
                                       [NotNullWhen(true)] out ElfDebugBinary? binary)
        {
            return ElfBinaryDiscoverer.IsGTestBinary(source,
                                                     this.loggerFactory,
                                                     out binary);
        }
        public async Task<string[]> GetTestListOutputAsync(string filePath, ElfDebugBinary binary, CancellationToken cancellationToken)
        {
            var dir = Guid.NewGuid().ToString("N").Substring(0, 8);
            dir = $"/tmp/{dir}";

            log.LogDebug("Using remote deployment directory: {dir}", dir);
            await client.RunCommandAsync(cancellationToken, "mkdir", "-p", dir)
                .ConfigureAwait(false);
            try
            {
                return await getListTestOutputAsync(dir,
                                                    filePath,
                                                    binary,
                                                    cancellationToken);
            }
            finally
            {
                try
                {
                    await client.RunCommandAsync(cancellationToken, "rm", "-rf", dir);
                }
                catch (Exception e)
                {
                    log.LogWarning(e, "Failed to remove deployment directory: {dir}", dir);
                }
            }
        }
        private async Task<string[]> getListTestOutputAsync(string dir,
                                                            string filePath,
                                                            ElfDebugBinary binary,
                                                            CancellationToken cancellationToken)
        {
            var mappings = getDependenciesToUpload(dir, filePath, binary);
            var exePath = PathUtils.CombineUnixPaths(dir, Path.GetFileName(filePath));
            mappings.Add(new(filePath, exePath));

            log.LogDebug("Copying files:\n{mappings}",
                string.Join("\n", mappings.Select(x => $"\t{x}")));

            await this.client
                .UploadAsync(mappings.ToArray(), cancellationToken)
                .ConfigureAwait(false);
            await client.RunCommandAsync(cancellationToken,
                                         "chmod",
                                         "+x",
                                         exePath);
            var outputs = await getListTestCommandOutput(exePath, cancellationToken)
                .ConfigureAwait(false);

            return outputs;
        }

        private async Task<string[]> getListTestCommandOutput(string exePath, CancellationToken cancellationToken)
        {

            var arguments = GoogleTestConstants.ListTestsOption;
            try
            {
                var output = await this.client
                .RunCommandAsync(exePath,
                                 arguments,
                                 cancellationToken: cancellationToken)
                .ConfigureAwait(false);

                log.LogDebug("Command output:\n{output}", output);

                var outputs = output.Split('\n');
                return outputs;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to get test list: {message}", ex.Message);
                throw;
            }
        }

        private List<SimplePathMapping> getDependenciesToUpload(string baseDir,
                                                                string filePath,
                                                                ElfDebugBinary binary)
        {
            var remoteFilePath = PathUtils.CombineUnixPaths(baseDir, filePath);
            var lst = new List<SimplePathMapping>
            {
                new(filePath, remoteFilePath)
            };
            var dependencies = binary.GetShLibDependencies();
            var directory = Path.GetDirectoryName(filePath)!;
            foreach(var dependency in dependencies)
            {
                var fname = Path.Combine(directory, dependency);
                if (File.Exists(fname))
                {
                    lst.Add(new(fname));
                }
            }
            return lst;
        }

        public Task<string?> MapRemoteFileAsync(string deploymentFile,
                                                string fullpath,
                                                CancellationToken cancellation)
        {
            return TaskResults.StringNull;
        }
    }
}
