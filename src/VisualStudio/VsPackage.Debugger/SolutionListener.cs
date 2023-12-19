using System.ComponentModel.Composition;
using System.Timers;
using LinuxDebugger.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using SourceMapModel = GoogleTestAdapter.Remote.SourceMap;
using Timer = System.Timers.Timer;

namespace GoogleTestAdapter.Remote.VsPackage.Debugger
{
    public interface ISolutionListener
    {
        bool IsEnabled { get; }
        DeploymentStrategy DeploymentMethod { get; set; }

        List<SourceMapModel>? GetSourceMap();
        List<ConnectionId>? GetConnections();
    }
    [Export(typeof(ISolutionListener))]
    internal sealed class SolutionListener : ISolutionListener
    {
        private readonly IServiceProvider services;
        private int nbLinuxProjectLoaded;
        private readonly object sync = new();
        private Dictionary<Guid, List<SourceMapModel>?>? sourceMaps;
        private Dictionary<Guid, ConnectionId>? connectionIds;

        public bool IsEnabled => this.nbLinuxProjectLoaded > 0;

        public DeploymentStrategy DeploymentMethod { get; set; }

        [ImportingConstructor]
        public SolutionListener([Import(typeof(SVsServiceProvider))] IServiceProvider services)
        {
            this.services = services;
            _ = this.initializeAsync();
        }

        private void removeMaps(IVsHierarchy hierarchy)
        {
            if (this.sourceMaps is not null)
            {
                var hier= hierarchy;
                var id = hier.GetProjectGuid();
                lock (sync)
                {
                    if (this.sourceMaps.ContainsKey(id))
                    {
                        this.sourceMaps.Remove(id);
                    }
                    this.connectionIds?.Remove(id);
                }
            }
        }

        private void onAfterCloseSolution(object sender, EventArgs e)
        {
            lock (sync)
            {
                this.sourceMaps?.Clear();
            }
        }

        private async Task initializeAsync()
        {
            var svc = this.services.GetService<SVsSolution, IVsSolution>();
            var flags = (uint)__VSENUMPROJFLAGS2.EPF_NOTFAULTED
                | (uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var nb = 0;
            foreach(var p in svc.GetAllProjects(flags).Where(VsExtensions.IsLinuxCppProject))
            {
                nb++;
                var hier = (IVsHierarchy)p;
                addMaps(hier);
            }

            this.nbLinuxProjectLoaded = nb;

            SolutionEvents.OnAfterCloseSolution += onSolutionClosed;
            SolutionEvents.OnBeforeUnloadProject += onProjectUnloaded;
            SolutionEvents.OnAfterLoadProject += onProjectLoaded;
            ProjectEvents.OnAfterProjectCfgChange += onProjectConfigChanged;
            ProjectEvents.OnBuildEnd += onProjectBuildEnd;
        }

        private void onProjectBuildEnd(object sender, HierarchyEventArgs e)
        {
            var guid = e.Hierarchy.GetProjectGuid();
            if (this.connectionIds?.TryGetValue(guid, out var id) == true)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    var cancellationToken = VsShellUtilities.ShutdownToken;
                    var hier = e.Hierarchy;
                    var item = await hier.GetProjectConnectionAsync().NoAwait();
                    var linkUpToDateTask = await item.IsUpToDateAsync(hier, cancellationToken);
                    id.UpToDate = linkUpToDateTask;
                });
            }
        }

        private void onProjectConfigChanged(object sender, HierarchyEventArgs e)
        {
            var hier = e.Hierarchy;
        }

        private void onProjectLoaded(object sender, LoadProjectEventArgs e)
        {
            if (e.RealHierarchy.IsLinuxCppProject())
            {
                this.nbLinuxProjectLoaded++;
                addMaps(e.RealHierarchy);
            }
        }
        private void addMaps(IVsHierarchy hier)
        {
            var cancellationToken = VsShellUtilities.ShutdownToken;
            var id = hier.GetProjectGuid();
            lock (sync)
            {
                (this.sourceMaps ??= new()).Add(id, null);
                this.connectionIds ??= new();
            }
            _ = Task.Run(async () =>
            {
                var item = await hier.GetProjectConnectionAsync(cancellationToken).NoAwait();
                _ = Task.Run(async () =>
                {
                    var tpathTask = hier.GetTargetPathAsync(cancellationToken);
                    var remotePathTask = item.GetRemoteTargetPathAsync(hier, cancellationToken);
                    var upToDateTask = item.IsUpToDateAsync(hier, cancellationToken);

                    await Task.WhenAll(tpathTask, remotePathTask, upToDateTask).ConfigureAwait(false);
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
#pragma warning disable VSTHRD103 // Call async methods when in an async method
                    var tpath = tpathTask.Result;
                    var upToDate = upToDateTask.Result;
                    var remotePath = remotePathTask.Result;
#pragma warning restore VSTHRD103 // Call async methods when in an async method
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                    Assumes.NotNullOrWhitespace(tpath);
                    if (tpath.IsPresent())
                    {
                        lock (sync)
                        {
                            this.connectionIds.Add(id, new()
                            {
                                Id = item.ConnectionId,
                                TargetPath = tpath,
                                RemotePath = remotePath,
                                UpToDate = upToDate
                            });
                        }
                    }
                });
                var rfs = await item.GetRemoteProjDirAsync(hier).NoAwait();
                lock (this.sourceMaps)
                {
                    if (this.sourceMaps.TryGetValue(id, out var lst))
                    {
                        this.sourceMaps[id] = (rfs.Mappings
                        ?.Select(x => new SourceMapModel
                        {
                            CompilerPath = x.CompilerPath,
                            EditorPath = x.EditorPath,
                        }) ?? Enumerable.Empty<SourceMapModel>()).ToList();
                    }
                }
            });
        }


        private void onProjectUnloaded(object sender, LoadProjectEventArgs e)
        {
            var hier = e.RealHierarchy;
            if (hier.IsLinuxCppProject())
            {
                this.decrementProjectCount();
                this.removeMaps(e.RealHierarchy);
            }
        }

        private void decrementProjectCount()
        {
            int nb;
            lock (this.sync)
            {
                nb = --nbLinuxProjectLoaded;
            }

            if (nb == 0)
            {
                var timer = new Timer
                {
                    Interval = TimeSpan.FromSeconds(30).TotalMilliseconds
                };
                timer.Elapsed += onShutdownTimerElapsed;
                timer.Start();
            }
        }

        private void onShutdownTimerElapsed(object sender, ElapsedEventArgs e)
        {
        }

        private void onSolutionClosed(object sender, EventArgs e)
        {
            this.nbLinuxProjectLoaded = 0;
            if (this.sourceMaps is not null)
            {
                lock (this.sourceMaps)
                {
                    this.sourceMaps.Clear();
                }
            }
        }

        List<ConnectionId>? ISolutionListener.GetConnections()
        {
            List<ConnectionId> connections;
            lock (this.sync)
            {
                if (this.connectionIds is null)
                    return null;
                connections = this.connectionIds
                    .Values
                    .ToList();
            }
            return connections;
        }

        List<SourceMapModel>? ISolutionListener.GetSourceMap()
        {

            List<SourceMapModel> all;
            lock (this.sync)
            {
                if (this.sourceMaps is null)
                    return null;
                all = this.sourceMaps.Values
                    .Where(x => x is not null)
                    .SelectMany(x => x)
                    .ToList();
            }
            return all.Count > 0 ? all : null;
        }
    }
}
