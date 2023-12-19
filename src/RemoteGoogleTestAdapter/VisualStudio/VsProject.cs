using EnvDTE;
using GoogleTestAdapter.Remote.Remoting;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using Sodiware.IO;

namespace GoogleTestAdapter.Remote.Adapter.VisualStudio
{
    internal sealed class VsProject : ISshClientConnection
    {
        private readonly VsIde ide;
        private readonly Project project;
        readonly Lazy<IVsBuildPropertyStorage> propertyStorage;
        readonly Lazy<string> configName, projectDir;
        readonly Lazy<string?> intDir;
        readonly Lazy<int> connectionId;
        IVsSshClient? sshClient;

        internal string? CopySourcesUpToDateFile
        {
            get
            {
                var intDir = this.intDir.Value;
                if (intDir.IsMissing())
                {
                    return null;
                }
                var connectionId = this.connectionId.Value;
                if (connectionId < 0) return null;

                // see _PrepareUpToDateChecks
                var fname = $"{connectionId}.CopySourcesUpToDateFile.tlog";
                return Path.Combine(intDir, fname);
            }
        }

        public string UniqueName
        {
            get
            {
                return this.project.UniqueName;
            }
        }

        internal VsProject(VsIde ide, Project project)
        {
            this.ide = ide;
            this.project = project;
            this.propertyStorage = new(() =>
            {
                var vsProject = this.ide.GetVsProject(this.project.UniqueName);
                return (IVsBuildPropertyStorage)vsProject;
            });
            this.configName = new(() =>
            {
                var configuration = this.project.ConfigurationManager.ActiveConfiguration.ConfigurationName;
                var platform = this.project.ConfigurationManager.ActiveConfiguration.PlatformName;
                return $"{configuration}|{platform}";
            });
            this.projectDir = new(() => Path.GetDirectoryName(this.project.FileName));
            this.intDir = new(() => GetPathPropertyValue("IntDir"));
            this.connectionId = new(GetBuildConnectionConfig);
            //this.remoteProjectDirFile = new(() => GetPropertyValue("RemoteProjectDirFile"));
        }

        public async Task<ISshClient> GetClientAsync(CancellationToken cancellationToken = default)
        {
            if (this.sshClient is not null)
            {
                return this.sshClient;
            }
            var id = this.connectionId.Value;
            this.sshClient = (id > 0
                ? await this.ide.GetSshClientAsync(id, cancellationToken)
                .ConfigureAwait(false)
                : await this.ide.GetSshClientAsync(cancellationToken)
                .ConfigureAwait(false));
            return this.sshClient;
        }

        private string? GetPropertyValue(string name,
            _PersistStorageType storageType = _PersistStorageType.PST_PROJECT_FILE)
        {
            var storage = this.propertyStorage.Value;
            if (ErrorHandler.Failed(storage.GetPropertyValue(name,
                                     this.configName.Value,
                                     (uint)storageType,
                                     out var value1)))
            {
                return null;
            }
            return value1;
        }

        private string? GetPathPropertyValue(string name,
            _PersistStorageType storage = _PersistStorageType.PST_PROJECT_FILE)
        {
            var value = GetPropertyValue(name, storage);
            if (value.IsMissing())
                return null;

            return Path.Combine(this.projectDir.Value, value);
        }

        private int GetBuildConnectionConfig()
        {
            //var remoteOutDir = GetPathPropertyValue("RemoteOutDir");
            //var targetPath = GetPathPropertyValue("TargetPath");
            //var outDir = GetPathPropertyValue("OutDir");
            var lastRemoteTargetFile = GetPropertyValue("LastRemoteTargetFile");
            int connectionId = -1;
            if (lastRemoteTargetFile.IsPresent() && File.Exists(lastRemoteTargetFile))
            {
                var str = File.ReadAllText(lastRemoteTargetFile);
                if (str.IsPresent() && int.TryParse(str, out connectionId))
                {
                }
            }

            if (connectionId < 0
                && !TryGetIConfigConnectionId(out connectionId)
                && !TryGetConnectionIdViaProject(out connectionId))
            {
            }
            if (connectionId < 0)
            {
                // TODO: Get default
            }
            return connectionId;
        }
        private bool TryGetIConfigConnectionId(out int connectionId)
        {
            var intDir = this.intDir.Value;
            if (intDir.IsPresent())
            {
                var path = Path.Combine(intDir, "iconfig.json");
                if (File.Exists(path))
                {
                    try
                    {
                        var text = File.ReadAllText(path);
                        var jo = JObject.Parse(text);
                        var hostId = jo.Property("host_identifier");
                        if (hostId?.HasValues == true)
                        {
                            connectionId = hostId.Value.Value<int>();
                            return true;
                        }
                    }
                    catch (Exception) { }
                }

            }
            connectionId = -1;
            return false;
        }
        private bool TryGetConnectionIdViaProject(out int connectionId)
        {
            try
            {
                var storage = this.propertyStorage.Value;
                if (storage is not null)
                {

                    var value = GetPropertyValue("RemoteTarget", _PersistStorageType.PST_USER_FILE);
                    if (value.IsPresent())
                    {
                        Assumes.NotNull(value);
                        var idStr = value.Split(';').First();
                        if (int.TryParse(idStr, out connectionId))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            connectionId = -1;
            return false;
        }

        internal async Task<(int ConnectionId, string? Output, string? TargetPath)> GetListTestOutputAsync(CancellationToken cancellationToken = default)
        {
            var remoteTargetPath = GetPropertyValue("RemoteTargetPath");
            if (remoteTargetPath.IsMissing())
            {
                return (0, string.Empty, null);
            }
            Assumes.NotNull(remoteTargetPath);
            var sshClient = await this.GetClientAsync(cancellationToken)
                .ConfigureAwait(false);
            var output = await sshClient.RunCommandAsync(remoteTargetPath,
                                                         GoogleTestConstants.ListTestsOption,
                                                         cancellationToken)
                .ConfigureAwait(false);

            return (sshClient.ConnectionId, output, remoteTargetPath);
        }

        internal async Task<SimplePathMapping?> GetFileMappingAsync(string fullPath, CancellationToken cancellationToken)
        {
            var mappings = await this.GetFileMappingAsync(cancellationToken).ConfigureAwait(false);
            foreach (var current in mappings)
            {
                if (string.Equals(current.Target, fullPath, StringComparison.Ordinal))
                {
                    return current;
                }
            }

            return null;
        }
        internal async Task<SimplePathMapping[]> GetFileMappingAsync(CancellationToken cancellationToken)
        {
            var list = new List<SimplePathMapping>();
            var file = this.CopySourcesUpToDateFile;
            ISshClient? client = null;
            if (file.IsPresent() && File.Exists(file))
            {
                var text = File.ReadAllText(file)
                    .Split(AdapterUtils.LineSeparatorChars, StringSplitOptions.RemoveEmptyEntries);
                foreach(var line in text)
                {
                    var items= line.Split(new[]{ '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length < 3)
                    {
                        continue;
                    }

                    client ??= await this
                            .GetClientAsync(cancellationToken)
                            .ConfigureAwait(false);
                    var localFilename = items[0];
                    var copiedDate = items[1];
                    var remoteDir = items[2];
                    var remoteFile = PathUtils.Unix.Combine(remoteDir, Path.GetFileName(localFilename));
                    DateTime lastWriteDate = DateTime.MinValue;
                    if (long.TryParse(copiedDate, out var cd))
                    {
                        lastWriteDate = new DateTime(cd);
                    }
                    remoteFile = await client
                        .ExpandAsync(remoteFile, cancellationToken)
                        .ConfigureAwait(false);
                    list.Add(new(localFilename, remoteFile)
                    {
                        LastWriteDate = lastWriteDate
                    });
                }
            }
            else
            {
            }
            return list.ToArray();
        }
    }
}