using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace GoogleTestAdapter.Remote.Adapter.VisualStudio
{
    internal static class DteExtensions
    {
        internal static Guid VsProjectKindCpp = new("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}");
        internal static bool IsCppProject(this Project project)
        {
            var cppProj = project.Kind.IsPresent()
                && Guid.TryParse(project.Kind, out var id)
                && id == VsProjectKindCpp;
            if (!cppProj || project.FileName.IsMissing()) return false;

            var ext = Path.GetExtension(project.FileName);
            return !string.Equals(ext, ".vcxitems", StringComparison.OrdinalIgnoreCase);
        }
        internal static string? GetTargetPath(this Project project, ILogger logger)
        {
            const int maxRetry = 5;
            string? targetPath = null;
            try
            {
                for (int tryCount = 0; tryCount < maxRetry; tryCount++)
                {
                    try
                    {
                        targetPath = doGetTargetPath(project);
                        break;
                    }
                    catch (COMException ex)
                    when (ex.HResult == VSConstants.RPC_E_SERVERCALL_RETRYLATER
                    && tryCount < (maxRetry - 1))
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while getting target path of project {project.Name}: {ex.Message}");
                return null;
            }

            return targetPath;
        }
        static string? doGetTargetPath(this Project project)
        {
            //var x = getTargetOutputWithoutDte(project);
            var props = project
                .ConfigurationManager
                ?.ActiveConfiguration
                ?.Properties;
            if (props is null)
            {
                return getTargetPathViaOutputGroups(project);
            }
            var dir = Path.GetDirectoryName(project.FullName);
            var targetPath = props.Item("OutputPath")?.Value as string;
            var targetName = props.Item("TargetName")?.Value as string;
            var targetExt = props.Item("TargetExt")?.Value as string;
            targetName += targetExt;
            if (targetPath is null || targetName is null || targetExt is null)
            {
                return getTargetPathViaOutputGroups(project);
            }

            //.OutputGroups.OfType <EnvDTE.OutputGroup>().First(x => x.CanonicalName == "Built");
            var fullPath = Path.Combine(dir, targetPath, targetName);
            return fullPath;
        }

        private static string? getTargetPathViaOutputGroups(Project project)
        {
            var outputFolders = new HashSet<string>();
            var builtGroup = project
                ?.ConfigurationManager
                ?.ActiveConfiguration
                ?.OutputGroups
                .OfType<OutputGroup>()
                .FirstOrDefault(x => string.Equals(x.CanonicalName, "Built", StringComparison.OrdinalIgnoreCase));
            if (builtGroup is null)
            {
                return null;
            }

            var fileUrls = ((object[])builtGroup.FileURLs).OfType<string>().ToArray();
            Debug.Assert(fileUrls.Length == 1);
            var uri = new Uri(fileUrls[0], UriKind.Absolute);
            return uri.LocalPath;
        }

        public static IServiceProvider? GetServiceProvider(this DTE dte)
        {
            var s = dte as IOleServiceProvider;
            if (s is not null)
            {
                return new ServiceProvider(s);
            }
            return null;
        }
    }
}

