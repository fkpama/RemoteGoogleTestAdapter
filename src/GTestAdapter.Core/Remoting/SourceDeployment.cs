using GoogleTestAdapter.Remote.Models;
using GoogleTestAdapter.Remote.Settings;
using GoogleTestAdapter.Remote.Symbols;
using liblinux.IO;
using PathHelper = Sodiware.IO.PathUtils;
using Microsoft.Extensions.Logging;
using Sodiware.Unix.DebugLibrary;
using ILogger = GoogleTestAdapter.Common.ILogger;

namespace GoogleTestAdapter.Remote.Remoting
{
    public abstract class SourceDeploymentBase : ISourceDeployment
    {
        protected readonly AdapterSettings settings;
        protected readonly ILogger log;

        protected SourceDeploymentBase(AdapterSettings settings, ILogger loggerFactory)
        {
            this.settings = settings;
            log = loggerFactory;
        }

        public abstract string? GetOutputProjectName(string exe);
        public abstract Task<string?> GetRemoteOutputAsync(TestCase testCase, CancellationToken cancellationToken);
        public abstract Task<TestListResult> GetTestListOutputAsync(string filePath, ElfDebugBinary binary, CancellationToken cancellationToken);
        public bool IsGoogleTestBinary(string source,
                                       [NotNullWhen(true)] out ElfDebugBinary? binary)
        {
            return ElfBinaryDiscoverer.IsGTestBinary(source, out binary);
        }


        public virtual Task<string?> MapRemoteFileAsync(string deploymentFile,
                                                string fullpath,
                                                CancellationToken cancellation)
        {
            var path = settings.SourceMap?.CompilerToEditorPath(fullpath);
            if (path.IsMissing())
            {
                log.LogWarning($"Mapping not found for path {fullpath} ({settings.SourceMap?.Count ?? 0} Mappings)");
            }
            return path.IsMissing() ? TaskResults.Null<string>() : Task.FromResult<string?>(path);
        }
    }
    public sealed class SourceDeployment : SourceDeploymentBase
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ISshClientRegistry registry;

        public SourceDeployment(AdapterSettings settings,
                                ISshClientRegistry registry,
                                ILogger logger,
                                ILoggerFactory loggerFactory)
            : base(settings, logger)
        {
            this.loggerFactory = loggerFactory.Safe();
            this.registry = registry;
        }

        //public override bool IsGoogleTestBinary(string source,
        //                               [NotNullWhen(true)] out ElfDebugBinary? binary)
        //{
        //    return ElfBinaryDiscoverer.IsGTestBinary(source,
        //                                             this.loggerFactory,
        //                                             out binary);
        //}
        //public override Task DeployAsync(string source, CancellationToken token)
        //{
        //    return Task.CompletedTask;
        //}
        public override async Task<TestListResult> GetTestListOutputAsync(string filePath, ElfDebugBinary binary, CancellationToken cancellationToken)
        {
            var client = await this.registry
                .GetClientAsync(filePath, cancellationToken)
                .ConfigureAwait(false);

            var target = this.settings.Connections?.Find(x => PathHelper.IsSamePath(filePath, x.TargetPath));
            if (target is not null)
            {
                try
                {
                    var remoteTimeStamp = await client.GetLastWriteTimeAsync(target.RemotePath, cancellationToken).NoAwait();
                    var localTime = PathHelper.GetLastWriteTime(filePath);
                    if (remoteTimeStamp >= localTime)
                    {
                        var outputs = await getListTestCommandOutput(target.RemotePath,
                                                         client,
                                                         cancellationToken)
                            .ConfigureAwait(false);

                        return new(client.ConnectionId, target.RemotePath, outputs);
                    }
                }
                catch (System.IO.IOException) { }
            }
            var dir = Guid.NewGuid().ToString("N").Substring(0, 8);
            dir = PathUtils.CombineUnixPaths(this.settings.RemoteDeploymentDirectory, dir);

            log.DebugInfo($"Using remote deployment directory: {dir}");
            await client.RunCommandAsync(cancellationToken, "mkdir", "-p", dir)
                .ConfigureAwait(false);
            try
            {
                return await getListTestOutputAsync(dir,
                                                    filePath,
                                                    binary,
                                                    client,
                                                    cancellationToken);
            }
            finally
            {
                if (this.settings.DiscoveryMode != AdapterMode.Execution)
                {
                    try
                    {
                        await deleteDirectory().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        log.LogWarning($"Failed to remove deployment directory: {dir}\n{e}");
                    }
                }
                else
                {
                    this.settings.AddCleanup(deleteDirectory);
                }

                async Task deleteDirectory()
                {
                    Assumes.NotNull(client);
                    Assumes.NotNull(dir);
                    log?.DebugInfo($"Cleaning up deployment directory: {dir}");
                    await client.RunCommandAsync(cancellationToken,
                                                 "rm",
                                                 "-rf",
                                                 dir);
                }
            }
        }
        private async Task<TestListResult> getListTestOutputAsync(string dir,
                                                            string filePath,
                                                            ElfDebugBinary binary,
                                                            ISshClient client,
                                                            CancellationToken cancellationToken)
        {
            var mappings = getDependenciesToUpload(dir, filePath, binary);
            var exePath = PathUtils.CombineUnixPaths(dir, Path.GetFileName(filePath));
            mappings.Add(new(filePath, exePath));

            log.DebugInfo($"Copying files:\n{string.Join("\n", mappings.Select(x => $"\t{x}"))}");

            await client
                .UploadAsync(mappings.ToArray(), cancellationToken)
                .ConfigureAwait(false);
            await client.RunCommandAsync(cancellationToken,
                                         "chmod",
                                         "+x",
                                         exePath);
            var outputs = await getListTestCommandOutput(exePath,
                                                         client,
                                                         cancellationToken)
                .ConfigureAwait(false);

            return new(client.ConnectionId, exePath, outputs);
        }

        private async Task<string[]> getListTestCommandOutput(string exePath,
                                                              ISshClient client,
                                                              CancellationToken cancellationToken)
        {

            var arguments = GoogleTestConstants.ListTestsOption;
            try
            {
                var output = await client
                .RunCommandAsync(exePath,
                                 arguments,
                                 cancellationToken: cancellationToken)
                .ConfigureAwait(false);

                log.DebugInfo($"Command output:\n{output}");

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
            foreach (var dependency in dependencies)
            {
                var fname = Path.Combine(directory, dependency);
                if (File.Exists(fname))
                {
                    lst.Add(new(fname));
                }
            }
            return lst;
        }

        public override string? GetOutputProjectName(string exe)
        {
            return null;
        }

        public override Task<string?> GetRemoteOutputAsync(TestCase testCase, CancellationToken cancellationToken)
        {
            var property = testCase.GetDeploymentProperty();
            if (property is null)
            {
                return TaskResults.Null<string>();
            }

            return Task.FromResult(property.RemoteExePath);
        }
    }
}
