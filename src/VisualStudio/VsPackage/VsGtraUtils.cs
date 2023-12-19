using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace GoogleTestAdapter.Remote.VisualStudio.Package
{
    internal static class VsGtraUtils
    {
        internal const string GoogleTestAdapterPackageId ="6fac3232-df1d-400a-95ac-7daeaaee74ac";
        internal const string TestExplorerContextGuid = "ec25b527-d893-4ec0-a814-d2c9f1782997";
        static readonly AsyncLazy<bool> s_gtaLoaded = new(async () =>
        {
            var asp = AsyncServiceProvider.GlobalProvider;
            var svc = await asp
            .GetServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>()
            .ConfigureAwait(false);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var guid = VSConstants.UICONTEXT.VCProject_guid;
            ErrorHandler.ThrowOnFailure(svc
                    .GetCmdUIContextCookie(ref guid, out var vcProjectUIContextCookie));

            guid = new Guid(TestExplorerContextGuid);
            ErrorHandler.ThrowOnFailure(svc
                    .GetCmdUIContextCookie(ref guid, out var testExplorerUiContextCookie));

            ErrorHandler.ThrowOnFailure(svc
                    .IsCmdUIContextActive(vcProjectUIContextCookie, out var pfActive));

            return Convert.ToBoolean(pfActive) && Convert.ToBoolean(vcProjectUIContextCookie);
        }, ThreadHelper.JoinableTaskFactory);

        internal static bool IsGTALoaded => s_gtaLoaded.GetValue(VsShellUtilities.ShutdownToken);
    }
}
