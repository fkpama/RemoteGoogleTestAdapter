using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Sodiware.VisualStudio;

namespace GoogleTestAdapter.Remote.VisualStudio.Package
{
    internal class DebuggerEventListener : IDebugEventCallback2
    {
        private IVsDebugger2 dbg;
        private uint dwcookie;
        static DebuggerEventListener s_instance;

        public DebuggerEventListener(IVsDebugger dbg)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.dbg = (IVsDebugger2)dbg;
            ErrorHandler.ThrowOnFailure(dbg.AdviseDebugEventCallback(this));
        }

        internal static async Task Initialize(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dbg = await package.GetServiceAsync<SVsShellDebugger, IVsDebugger>();
            var listener = new DebuggerEventListener(dbg);
            s_instance = listener;
        }

        public int Event(IDebugEngine2 pEngine,
                         IDebugProcess2 pProcess,
                         IDebugProgram2 pProgram,
                         IDebugThread2 pThread,
                         IDebugEvent2 pEvent,
                         ref Guid riidEvent,
                         uint dwAttrib)
        {
            if (pEngine is not null)
            {
                processEvent(pEngine).FileAndForget();
            }
            else if (pProgram is not null)
            {
                ErrorHandler.ThrowOnFailure(pProgram.GetEngineInfo(out var name, out var id));
                System.IO.File.AppendAllText(@"F:\Temp\DebugEngines.log", $"{name}: {id}\n");
            }
            return VSConstants.S_OK;
        }

        async Task processEvent(IDebugEngine2 pEngine)
        {
            ErrorHandler.ThrowOnFailure(pEngine.GetEngineId(out var id));
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ErrorHandler.ThrowOnFailure(this.dbg.GetEngineName(ref id, out var name));
            System.IO.File.AppendAllText(@"F:\Temp\DebugEngines.log", $"{name}: {id}\n");
        }
    }
}
