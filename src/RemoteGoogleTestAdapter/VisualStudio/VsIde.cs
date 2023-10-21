using System.Collections;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using RemoteGoogleTestAdapter.VisualStudio;
using Sodiware.IO;

namespace RemoteGoogleTestAdapter.IDE
{

    public sealed class VsIde
    {
        private static JoinableTaskFactory? s_joinableTaskFactory;
        private readonly Dictionary<string, VsProject> projects = new();
        private readonly DTE dte;
        private readonly ILogger logger;
        private readonly ServiceProvider serviceProvider;


        public VsIde(DTE ide, ILogger logger)
        {
            this.dte = ide;
            this.logger = logger;
            this.serviceProvider = new ServiceProvider(ide as IOleServiceProvider);
        }

        public static JoinableTaskFactory JoinableTaskFactory
        {
            get
            {
                if (s_joinableTaskFactory is null)
                {
                    JoinableTaskFactory? taskFactory;
                    try
                    {
                        taskFactory = ThreadHelper.JoinableTaskFactory;
                    }
                    catch
                    {
#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
                        var context = new JoinableTaskContext();
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
                        taskFactory = new JoinableTaskFactory(context);
                    }
                    s_joinableTaskFactory = taskFactory;
                }
                return s_joinableTaskFactory;
            }
        }

        internal static bool TryGetInstance(int id, ILogger logger, out VsIde? vsIde)
        {
            try
            {
                vsIde = VSAutomation.GetIDEInstance(id, logger);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error getting visual studio instance for PID: {ex.Message}");
                vsIde = null;
                return false;
            }
        }

        internal VsProject? GetProjectForOutputPath(string filePath, bool filenameOnly = true)
        {
            filePath = normalize(filePath);
            if (this.projects.TryGetValue(filePath, out var vsProject))
            {
                return vsProject;
            }
            var projects = this.dte.Solution.Projects;
            var count = projects.Count;
            for(int i = 0; i < count; i++)
            {
                var project = projects.Item(i + 1);
                Assumes.NotNull(project);
                if (project is null)
                {
                    continue;
                }

                if (!project.IsCppProject())
                {
                    continue;
                }

                string? targetPath;
                var name = project.Name;
                try
                {
                    targetPath = project.GetTargetPath(this.logger);
                    if (targetPath.IsMissing())
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error getting target path for project: {name}\n{ex}");
                    continue;
                }
                Assumes.NotNull(targetPath);
                if (filenameOnly)
                {
                    var fname = Path.GetFileName(targetPath);
                    var fname2 = Path.GetFileName(filePath);
                    if(string.Equals(fname, fname2, StringComparison.OrdinalIgnoreCase))
                    {
                        vsProject = new(this, project);
                    }
                }
                else
                {
                    if (PathUtils.IsSamePath(targetPath, filePath))
                    {
                        vsProject = new(this, project);
                    }
                }

                if (vsProject is not null)
                {
                    this.projects.Add(filePath, vsProject);
                    return vsProject;
                }
            }

            return null;
        }

        private static string normalize(string filePath)
        {
            return Path.GetFullPath(filePath).ToUpperInvariant();
        }

        internal Task<IVsSshClient> GetSshClientAsync(int connectionId, CancellationToken cancellationToken)
        {
            //var conn = this.connectionManager.Value;
            var cli = ConnectionFactory.CreateClient(connectionId, JoinableTaskFactory, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(cli);
        }

        internal Task<IVsSshClient> GetSshClientAsync(CancellationToken cancellationToken)
        {
            var cli = ConnectionFactory.CreateClient(JoinableTaskFactory, cancellationToken);
            //return await conn.GetDefaultConnectionAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(cli);
        }

        internal IVsHierarchy GetVsProject(string uniqueName)
        {
            var solution = this.serviceProvider.GetService<IVsSolution>();
            IVsHierarchy result;
            if (ErrorHandler.Failed(solution
                .GetProjectOfUniqueName(uniqueName, out result)))
            {
                this.logger.DebugError($"Failed to get IVsHierarchy for project {uniqueName}");
            }
            return result;
        }
    }
}