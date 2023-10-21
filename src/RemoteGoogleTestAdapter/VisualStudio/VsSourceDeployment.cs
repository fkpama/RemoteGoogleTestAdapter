using System.Runtime.CompilerServices;
using GTestAdapter.Core;
using GTestAdapter.Core.Settings;
using RemoteGoogleTestAdapter.IDE;
using Sodiware.Unix.DebugLibrary;

namespace RemoteGoogleTestAdapter.VisualStudio
{
    internal class VsSourceDeployment : ISourceDeployment
    {
        private readonly VsIde vsIde;
        private readonly AdapterSettings settings;
        private readonly ILogger<VsSourceDeployment> log;

        public VsSourceDeployment(VsIde vsIde,
                                  AdapterSettings settings,
                                  ILogger<VsSourceDeployment> log)
        {
            this.vsIde = vsIde;
            this.settings = settings;
            this.log = log;
        }

        public async Task<string[]> GetTestListOutputAsync(string filePath,
                                                     ElfDebugBinary binary,
                                                     CancellationToken cancellationToken)
        {
            var project = this.vsIde
                .GetProjectForOutputPath(filePath,
                                         this.settings.OverrideSource.IsPresent());
            if (project is not null)
            {
                var output = await project
                    .GetListTestOutputAsync(cancellationToken)
                    .ConfigureAwait(false);
                log.LogDebug("Test list output:\n{output}", output);
                return output.Trim().Split(AdapterUtils.LineSeparatorChars,
                                           StringSplitOptions.RemoveEmptyEntries);
            }
            return Array.Empty<string>();
        }

        public bool IsGoogleTestBinary(string source,
                                       [NotNullWhen(true)] out ElfDebugBinary? binary)
        {
            return ElfBinaryDiscoverer.IsGTestBinary(source, out binary);
        }

        public async Task<string?> MapRemoteFileAsync(string deploymentFile,
                                                string fullpath,
                                                CancellationToken cancellation)
        {
            var project = this.vsIde
                .GetProjectForOutputPath(deploymentFile,
                this.settings.OverrideSource.IsPresent());

            if (project is null)
            {
                return null;
            }

            var mapping  = await project
                .GetFileMappingAsync(fullpath, cancellation)
                .ConfigureAwait(false);

            return mapping?.Source;
        }
    }
}
