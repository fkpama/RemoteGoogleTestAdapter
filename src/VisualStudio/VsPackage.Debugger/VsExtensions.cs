using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleTestAdapter.Remote.VsPackage.Debugger
{
    internal static class VsExtensions
    {
        internal static bool IsLinuxCppProject(this IVsProject hier)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return IsLinuxCppProject((IVsHierarchy)hier);
        }
        internal static bool IsLinuxCppProject(this IVsHierarchy hier)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //PackageUtilities.IsCapabilityMatch(hier, "SharedAssetsProject")
            return hier
                .IsCapabilityMatch("LinuxRemoteNative") && !hier.IsSharedAssetsProject();
        }
    }
}
