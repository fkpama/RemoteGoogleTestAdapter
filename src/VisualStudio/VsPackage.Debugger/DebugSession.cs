using GoogleTestAdapter.Remote.Debugger;
using LinuxDebugger.VisualStudio;
using LinuxDebugger.VisualStudio.Debugger;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Sodiware.IO;
using Sodiware.VisualStudio.Logging;
using Sodiware.VisualStudio.Utils;

namespace GoogleTestAdapter.Remote.VsPackage.Debugger
{
    internal sealed partial class DebugSession : IDebugSession
    {
        private readonly AsyncLazy<IVsHierarchy> project;
        private readonly IDebuggerLauncher session;
        private readonly JoinableTaskFactory taskFactory;
        private readonly ILogger log;

        public DebugLauncher Launcher { get; }
        public IVsSshClient SshClient => this.Launcher.Client;
        public TestLaunchBatch Parameters { get; }

        public string Arguments => this.Parameters.CommandLine;

        public DebugSession(DebugLauncher launcher,
                            IDebuggerLauncher session,
                            string projectName,
                            TestLaunchBatch parameters,
                            IVsSolution solution,
                            JoinableTaskFactory taskFactory,
                            ILogger log)
        {
            this.Launcher = launcher;
            this.session = session;
            this.Parameters = parameters;
            this.project = new(async () =>
            {
                await taskFactory.SwitchToMainThreadAsync();
                ErrorHandler.ThrowOnFailure(solution
                    .GetProjectOfUniqueName(projectName, out var hierarchy));
                Assumes.NotNull(hierarchy);
                return hierarchy;
            }, taskFactory);
            this.taskFactory = taskFactory;
            this.log = log;
        }

        //private async Task<(string ProjectDirectory, string RemoteProjectDirectory)>
        //    getRemoteProjDirAsync(IVsHierarchy project,
        //                              List<SourceMap> sourceMap,
        //                              CancellationToken cancellationToken)
        //{
        //    var remoteTarget = await project
        //        .GetBuildPropertyValueAsync("RemoteProjectDir", cancellationToken).NoAwait();
        //    var remoteProjectRelDir = await project
        //        .GetBuildPropertyValueAsync("RemoteProjectRelDir", cancellationToken)
        //        .ConfigureAwait(false);

        //    var projectDir = await project
        //        .GetProjectDirectoryAsync(cancellationToken)
        //        .ConfigureAwait(false);

        //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        //    if (project.IsProjectImportingSharedAssets())
        //    {
        //        var rootTarget = remoteTarget;
        //        //remoteTarget = PathUtils.Unix.Combine(remoteTarget, remoteProjectRelDir);
        //        var mappings = RemotePathHelper.MapProjectDirectory(projectDir,
        //            remoteTarget,
        //            project
        //            .GetSharedItemsImportFullPaths()
        //            .Select(x => Path.GetDirectoryName(x)));
        //        await TaskScheduler.Default;
        //        remoteTarget = mappings.RemoteProjectDirectory;
        //        foreach(var mapping in mappings.SharedItemsMappings)
        //        {
        //            var remoteShDir = await SshClient
        //                .ExpandAsync(mapping.Remote, cancellationToken)
        //                .NoAwait();
        //            sourceMap.Add(new(mapping.Local.EnsureTrailingBackslash(),
        //                              remoteShDir.EnsureTrailingSlash()));
        //        }
        //    }

        //    if (ThreadHelper.CheckAccess())
        //        await TaskScheduler.Default;
        //    var remote = await SshClient.ExpandAsync(remoteTarget, cancellationToken).NoAwait();
        //    sourceMap.Add(new(remoteTarget.EnsureTrailingBackslash(), remote.EnsureTrailingSlash()));
        //    return (projectDir, remote);
        //}

        private async Task<string> getRemoteTargetPathAsync(IVsHierarchy project, CancellationToken cancellationToken)
        {

            var remoteTarget = await project
                .GetBuildPropertyValueAsync("RemoteTargetPath", cancellationToken)
                .ConfigureAwait(false);

            if (remoteTarget.IsMissing())
            {
                remoteTarget = await computeRemoteTargetAsync(project, cancellationToken).NoAwait();
            }

            remoteTarget = await this.SshClient
                .ExpandAsync(remoteTarget, cancellationToken)
                .NoAwait();

            return remoteTarget;
        }
        private static async Task<string> computeRemoteTargetAsync(
            IVsHierarchy vsProject,
            CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var project = (IVsBuildPropertyStorage)vsProject;
            var remoteProjectRelDir = await project
                .GetBuildPropertyValueAsync("RemoteProjectRelDir", cancellationToken)
                .ConfigureAwait(false);
            var remoteRootDir = await project
                .GetBuildPropertyValueAsync("RemoteRootDir", cancellationToken)
                .ConfigureAwait(false);
            var remoteOutRelDir = await project
                .GetBuildPropertyValueAsync("RemoteOutRelDir", cancellationToken)
                .ConfigureAwait(false);
            var targetName = await project
                    .GetBuildPropertyValueAsync("TargetName", cancellationToken)
                    .ConfigureAwait(false);
            var targetExt = await project
                    .GetBuildPropertyValueAsync("TargetExt", cancellationToken)
                    .ConfigureAwait(false);
            var fname = $"{targetName}{targetExt}";
            return PathUtils.Unix.Combine(remoteRootDir.ThrowIfNullOrEmpty(),
                                          remoteProjectRelDir.ThrowIfNullOrEmpty(),
                                          remoteOutRelDir.ThrowIfNullOrEmpty(),
                                          fname.ThrowIfNullOrEmpty());
        }

        public async Task<RemoteLaunchSettings[]> QueryDebugTargetsAsync(CancellationToken cancellationToken)
        {
            var project = await this.project
                .GetValueAsync(cancellationToken)
                .ConfigureAwait(false);

            var t1 = getRemoteTargetPathAsync(project, cancellationToken);
            var t2 = this.SshClient.GetRemoteProjDirAsync(project, cancellationToken);

            await Task.WhenAll(t1, t2).NoAwait();
#pragma warning disable VSTHRD103 // Call async methods when in an async method
            var remoteTargetPath = t1.Result;
            var rfs = t2.Result;
#pragma warning restore VSTHRD103 // Call async methods when in an async method

            var target = new RemoteLaunchSettings
            {
                ConnectionInfo = this.SshClient.ConnectionInfo,
                CurrentDirectory = rfs.RemoteProjectDirectory,
                Executable = remoteTargetPath,
                Arguments = $"{this.Arguments} --gtest_color=no",
                Project = project,
                SendToOutputWindow = true,
                SourceMap = rfs.Mappings
            };
            return new[] { target };
        }
    }
}
