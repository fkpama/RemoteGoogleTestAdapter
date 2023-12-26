using GoogleTestAdapter.Model;
using GoogleTestAdapter.Remote.Models;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Settings;
using GoogleTestAdapter.Remote.Symbols;
using Sodiware.Unix.DebugLibrary;

namespace GoogleTestAdapter.Remote.Adapter.VisualStudio
{
    internal class VsSourceDeployment : SourceDeploymentBase
    {
        private readonly VsIde vsIde;
        private readonly ISshClientRegistry registry;
        private readonly ILoggerFactory loggerFactory;
        private SourceDeployment? fallback;

        public VsSourceDeployment(VsIde vsIde,
                                  AdapterSettings settings,
                                  ISshClientRegistry registry,
                                  ILogger log,
                                  ILoggerFactory loggerFactory)
            : base(settings, log)
        {
            this.vsIde = vsIde;
            this.registry = registry;
            this.loggerFactory = loggerFactory;
        }

        public override async Task<TestListResult> GetTestListOutputAsync(string filePath,
                                                           ElfDebugBinary binary,
                                                           CancellationToken cancellationToken)
        {
            var flags = TestMethodDescriptorFlags.ExternalDeployment;
            var project = this.vsIde
                .GetProjectForOutputPath(filePath,
                                         this.settings.OverrideSource.IsPresent());
            if (project is not null)
            {
                var (connectionId, output, targetPath) = await project
                    .GetListTestOutputAsync(cancellationToken)
                    .ConfigureAwait(false);
                log.DebugInfo($"Test list output:\n===\n{output}");
                if (output.IsPresent())
                {
                    Assumes.NotNull(output);
                    Assumes.NotNull(targetPath);
                    var ar = output.Trim().Split(AdapterUtils.LineSeparatorChars,
                                           StringSplitOptions.RemoveEmptyEntries);
                    return new(connectionId, targetPath, ar, flags);
                }
            }

            this.fallback ??= new(this.settings,
                                  this.registry,
                                  this.log,
                                  this.loggerFactory);
            return await this.fallback.GetTestListOutputAsync(filePath,
                                                        binary,
                                                        cancellationToken);
        }

        //public async Task<string?> MapRemoteFileAsync(string deploymentFile,
        //                                        string fullpath,
        //                                        CancellationToken cancellation)
        //{
        //    //var project = this.vsIde
        //    //    .GetProjectForOutputPath(deploymentFile,
        //    //    this.settings.OverrideSource.IsPresent());

        //    //if (project is null)
        //    //{
        //    //    return null;
        //    //}

        //    //var mapping  = await project
        //    //    .GetFileMappingAsync(fullpath, cancellation)
        //    //    .ConfigureAwait(false);

        //    //if (!mapping.HasValue || mapping.Value.Source.IsMissing())
        //    //{
        //        return this.settings.SourceMap?.CompilerToEditorPath(fullpath);
        //    //}

        //    //return mapping?.Source;
        //}

        public override string? GetOutputProjectName(string exe)
        {
            if (this.settings.OverrideSource.IsPresent())
            {
                Assumes.NotNull(this.settings.OverrideSource);
                exe = this.settings.OverrideSource;
            }
            var project = this.vsIde.GetProjectForOutputPath(exe);
            return project?.UniqueName;
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
