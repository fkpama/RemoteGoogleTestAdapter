global using LinuxDebugger;
global using Sodiware;
global using GoogleTestAdapter.Common;
global using IMSLogger = Microsoft.Extensions.Logging.ILogger;
global using Microsoft.Extensions.Logging;
global using ILogger = GoogleTestAdapter.Common.ILogger;
global using Assumes = Sodiware.Assumes;
global using LinuxDebugger.VisualStudio;
global using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
global using SysDebugger = System.Diagnostics.Debugger;

global using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
// see: https://learn.microsoft.com/en-us/visualstudio/extensibility/migration/breaking-api-list?view=vs-2022
#if true
global using IOleAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.COMAsyncServiceProvider.IAsyncServiceProvider;
#else
global using IOleAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.IAsyncServiceProvider;
#endif
