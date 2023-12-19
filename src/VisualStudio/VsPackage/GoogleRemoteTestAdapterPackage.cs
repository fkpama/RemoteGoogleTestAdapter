using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using GoogleTestAdapter.Remote;
using GoogleTestAdapter.Remote.VisualStudio.Package;
using GoogleTestAdapter.Remote.VisualStudio.Package.OptionsPage;
using GoogleTestAdapter.Remote.VsPackage.Debugger;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace GTestAdapter.Package
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(GoogleRemoteTestAdapterPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    //[ProvideAutoLoad(VSConstants.UICONTEXT.VCProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContext_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideUIContextRule(UIContext_string,
        name: "Google test project auto load",
        expression: "HasCrossPlatformProject & TestExplorer",
        termNames: new[]
        {
            "HasCrossPlatformProject",
            "TestExplorer"
        },
        termValues: new[]
        {
            "SolutionHasProjectCapability:LinuxRemoteNative",
            VsGtraUtils.TestExplorerContextGuid
        }
        //, delay: 3000
        )]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(CrossPlatformOptionPage),
        CrossPlatformOptionPage.OptionsCategoryName,
        "Cross Platform",
        110,
        113,
        false,
        Sort = 500)]
    public sealed class GoogleRemoteTestAdapterPackage : AsyncPackage
    {
        public const string UIContext_string= "e8d7085d-56ea-41fb-9aac-380a56fd9cf1";
        /// <summary>
        /// GoogleRemoteTestAdapterPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "4578f827-e6dd-407c-9f71-57fd53597180";
        private CrossPlatformOptionPage dialogPage;

        static GoogleRemoteTestAdapterPackage()
        {
            CommonVSUtils.RegisterAssemblyLoad();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleRemoteTestAdapterPackage"/> class.
        /// </summary>
        public GoogleRemoteTestAdapterPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Guid gtaPackageGuid = new(VsGtraUtils.GoogleTestAdapterPackageId);

            var componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel2>()
                .ConfigureAwait(false);
            var listener = componentModel.GetService<ISolutionListener>();
            this.dialogPage = (CrossPlatformOptionPage)this.GetDialogPage(typeof(CrossPlatformOptionPage));
            listener.DeploymentMethod = (DeploymentStrategy)this.dialogPage.DeploymentStrategy;
            var shell = await this.GetServiceAsync<SVsShell, IVsShell>().ConfigureAwait(false);
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await DebuggerCommands.InitializeAsync(this);
            await DebuggerEventListener.Initialize(this);

            if (ErrorHandler.ThrowOnFailure(shell.IsPackageLoaded(ref gtaPackageGuid, out _),
                VSConstants.E_FAIL) == VSConstants.E_FAIL)
            {
                //var logger = Log
                ErrorHandler.ThrowOnFailure(shell.IsPackageInstalled(ref gtaPackageGuid, out var pkg));
                if (!Convert.ToBoolean(pkg))
                {
                    throw new NotImplementedException();
                }
                ErrorHandler.ThrowOnFailure(shell.LoadPackage(ref gtaPackageGuid, out _));
            }
        }

        #endregion
    }
}
