using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
using SysDebugger = System.Diagnostics.Debugger;
using GoogleTestAdapter.Remote.Debugger;
using GoogleTestAdapter.Remote.VsPackage.Debugger.Client;
using System.Runtime.InteropServices;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Microsoft.VisualStudio;
using Sodiware.VisualStudio;

namespace GoogleTestAdapter.Remote.VisualStudio.Package
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DebuggerCommands : IOleCommandTarget
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a1028ddc-d70b-47bc-ad28-6c75efc02a89");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;
        private uint cookie;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebuggerCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private DebuggerCommands(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(GrtaDebuggerCommands.CommandSet, GrtaDebuggerCommands.CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID)
            {
                MatchedCommandId = GrtaDebuggerCommands.CommandId,
                Text = string.Empty,
                Visible = false,
                ParametersDescription = "$",
                Enabled =true,
                Supported = true
            };
            menuItem.BeforeQueryStatus += (o, e) => beforeQueryStatus((OleMenuCommand)o);
            commandService.AddCommand(menuItem);
        }

        private void beforeQueryStatus(OleMenuCommand o)
        {
            //var dte = this.package.GetService<SDTE, DTE>() as IOleServiceProvider;
            ThreadHelper.ThrowIfNotOnUIThread();
            //if (VsShellUtilities.IsInAutomationFunction(ServiceProvider.GlobalProvider))
            //{
                o.Enabled = true;
                o.Supported = true;
            //}
            //else
            //{
                o.Enabled = true;
                o.Supported = true;
            //}
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DebuggerCommands Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider AsyncServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in DebuggerCommands's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var reg = await package.GetServiceAsync<SVsRegisterPriorityCommandTarget, IVsRegisterPriorityCommandTarget>();
            Instance = new DebuggerCommands(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (e is not OleMenuCmdEventArgs evtArgs)
            {
                // TODO: Show error
                return;
            }
            if (evtArgs.InValue is null)
            {
                // TODO: Show error
                return;
            }
            var parameter = evtArgs.InValue;
            string message;
            // see: https://www.codeproject.com/Articles/720329/Visual-Studio-Extensions-from-Add-in-to-VSPackage
            try
            {
                message = Convert.ToString(parameter);
            }
            catch (Exception)
            {
                // TODO: Show error
                return;
            }

            // Show a message box to prove we were here
            //VsShellUtilities.ShowMessageBox(
            //    this.package,
            //    message,
            //    title,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            int succeeded = VSConstants.S_OK;
            try
            {
                Execute(message);
            }
            catch(Exception ex)
            {
                succeeded = ex.HResult;
            }
            finally
            {
                if (evtArgs.OutValue != IntPtr.Zero)
                {
                    Marshal.GetNativeVariantForObject(succeeded, evtArgs.OutValue);
                }
            }
        }

        private void Execute(string text)
        {
            var client = DebuggerClientLauncher
                .LaunchFromJson(this.package, text);
            client.StartAsync().FileAndForget();
        }
        /// <summary>
        /// Used to determine if the shell is querying for the parameter list.
        /// </summary>
        static private bool IsQueryParameterList(IntPtr pvaOut, uint nCmdexecopt)
        {
            ushort lo = (ushort)(nCmdexecopt & (uint)0xffff);
            ushort hi = (ushort)(nCmdexecopt >> 16);
            if (lo == (ushort)OLECMDEXECOPT.OLECMDEXECOPT_SHOWHELP)
            {
                if (hi == VsMenus.VSCmdOptQueryParameterList)
                {
                    if (pvaOut != IntPtr.Zero)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == GrtaDebuggerCommands.CommandSet)
            {
                SysDebugger.Break();
                return (int)OleConstants.MSOCMDF_SUPPORTED | (int)OleConstants.MSOCMDF_ENABLED;
            }
            return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
        }

        public int Exec(ref Guid pguidCmdGroup,
                        uint nCmdID,
                        uint nCmdexecopt,
                        IntPtr pvaIn,
                        IntPtr pvaOut)
        {
            if (pguidCmdGroup == GrtaDebuggerCommands.CommandSet)
            {
                if (pvaIn != IntPtr.Zero)
                {
                    var arg = Marshal.GetObjectForNativeVariant(pvaIn);
                }
                SysDebugger.Break();
                return (int)OleConstants.MSOCMDF_SUPPORTED | (int)OleConstants.MSOCMDF_ENABLED;
            }
            return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
        }
    }
}
