using System.Reflection;
using EnvDTE;
using GoogleTestAdapter.Remote.Debugger;
using GoogleTestAdapter.Remote.Remoting;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Sodiware.IO;

namespace GoogleTestAdapter.Remote.Adapter.VisualStudio
{

    public sealed class VsIde
    {
        private static JoinableTaskFactory? s_joinableTaskFactory;
        private static VsIde? s_instance;
        private readonly Dictionary<string, VsProject> projects = new();
        private readonly DTE dte;
        private readonly ISshClientRegistry registry;
        private readonly ILogger logger;
        private readonly ServiceProvider serviceProvider;

        static VsIde()
        {
            CommonVSUtils.RegisterAssemblyLoad();
        }


        #region Properties

        public static JoinableTaskFactory JoinableTaskFactory
        {
            get
            {
                if (s_joinableTaskFactory is null)
                {
                    //JoinableTaskFactory? taskFactory;
                    //try
                    //{
                    //    taskFactory = ThreadHelper.JoinableTaskFactory;
                    //    s_joinableTaskFactory = taskFactory;
                    //}
                    //catch
                    //{
                    CreateLocalTaskFactory();
                    //}
                }
                return s_joinableTaskFactory;
            }
        }

        #endregion Properties

        public VsIde(DTE ide, ISshClientRegistry registry, ILogger logger)
        {
            this.dte = ide;
            this.registry = registry;
            this.logger = logger;
            this.serviceProvider = new ServiceProvider(ide as IOleServiceProvider);
        }



        internal static void SetCurrentInstance(VsIde vsIde)
        {
            s_instance = vsIde;
        }
        internal static VsIde? GetInstance()
        {
            return s_instance;
        }
        internal static bool TryGetInstance(int id,
                                            ISshClientRegistry registry,
                                            ILogger logger,
                                            out VsIde? vsIde)
        {
            try
            {
                vsIde = VSAutomation.GetIDEInstance(id, registry, logger);
                return vsIde is not null;
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
            filePath = GUtils.NormalizePath(filePath);
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

                var name = project.Name;
                string? targetPath;
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
                    this.registry.Register(filePath, vsProject);
                    this.projects.Add(filePath, vsProject);
                    return vsProject;
                }
            }

            return null;
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

        internal T? LaunchCommand<T>(int commandId, string commandArgs, TimeSpan? timeout = null)
            => LaunchCommand<T>(GrtaDebuggerCommands.CommandSet, commandId, commandArgs, timeout);
        internal T? LaunchCommand<T>(Guid commandSetId, int commandId, string commandArgs, TimeSpan? timeout = null)
        {
            object? args1 = commandArgs;
            object? args2 = null;
            var guid = commandSetId.ToString("D");
            var solutionName = Path.GetFileNameWithoutExtension(this.dte.Solution.FileName);
            logger.LogInfo($"Launching command {guid}|{commandId} ({solutionName}|{PathUtils.MakeRelative(Environment.CurrentDirectory, this.dte.Solution.FileName)})");
            logger.DebugInfo($"Command arguments [Timeout: {timeout}] {commandArgs}");
            try
            {
                var evt = new ManualResetEventSlim();
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    this.dte.Commands.Raise(guid, commandId, ref args1, ref args2);
                    logger.DebugInfo($"Command successfully executed. [Out: {args2}]");
                    evt.Set();
                }, null);
                if(!evt.Wait(timeout ?? Timeout.InfiniteTimeSpan))
                {
                    throw new TimeoutException();
                }
            }
            catch (Exception ex)
            when (ex is not TimeoutException)
            {
                logger.LogError(ex, $"Error executing command: {ex.Message}");
                throw;
            }
            //this.dte.ExecuteCommand("GoogleRemoteTestAdapter.LaunchDebugger", commandArgs ?? string.Empty);
            return args2 is null ? default : (T)args2;
        }

        internal IVsHierarchy GetVsProject(string uniqueName)
        {
            var solution = this.serviceProvider.GetService<IVsSolution>();
            if (ErrorHandler.Failed(solution
                .GetProjectOfUniqueName(uniqueName, out var result)))
            {
                this.logger.DebugError($"Failed to get IVsHierarchy for project {uniqueName}");
            }
            return result;
        }

        [MemberNotNull(nameof(s_joinableTaskFactory))]
        internal static void CreateLocalTaskFactory()
        {
#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
            var context = new JoinableTaskContext();
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
            s_joinableTaskFactory = new JoinableTaskFactory(context);
        }
    }
}