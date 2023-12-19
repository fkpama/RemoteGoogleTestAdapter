using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using EnvDTE;
using GoogleTestAdapter.Remote.Remoting;
using Microsoft.VisualStudio;

namespace GoogleTestAdapter.Remote.Adapter.VisualStudio
{
    internal static class VSAutomation
    {
        [DllImport("ole32.dll")]
        static extern int GetRunningObjectTable(int reserved,
                                                out IRunningObjectTable prot);

        [DllImport("ole32.dll")]
        static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        public static VsIde? GetIDEInstance(int pid,
                                            ISshClientRegistry registry,
                                            ILogger logger)
        {
            try
            {
                return doGetIDEInstance(pid, registry, logger);
            }
            catch(Exception ex)
            {
                logger.LogError($"Error getting IDE instance: {ex}");
                return null;
            }
        }

        static readonly Regex s_vsDteRegex = new(@"!VisualStudio\.DTE\.\d+\.\d+:(?<pid>\d+)", RegexOptions.Compiled);
        static unsafe VsIde? doGetIDEInstance(int pid,
                                              ISshClientRegistry registry,
                                              ILogger logger)
        {
            ErrorHandler.ThrowOnFailure(GetRunningObjectTable(0, out var rot));

            rot.EnumRunning(out var enumerator);

            IMoniker[] moniker = new IMoniker[1];
            int fetched = 0;
            IntPtr ptr = (IntPtr)(&fetched);
            while (ErrorHandler.Succeeded(enumerator.Next(1, moniker, ptr))
                && fetched > 0)
            {
                if(ErrorHandler.Succeeded(CreateBindCtx(0, out var ctx)))
                {
                    string displayName;
                    try
                    {
                        moniker[0].GetDisplayName(ctx, null, out displayName);
                        if (displayName.IsMissing())
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        //logger.DebugInfo("Failed to get display name");
                        continue;
                    }
                    if (!displayName.StartsWith("!VisualStudio.DTE", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }


                    var match = s_vsDteRegex.Match(displayName);
                    if (!match.Success)
                    {
                        logger.DebugWarning($"Failed to match DTE PID regex agains VS Instance {displayName}");
                        continue;
                    }

                    var processPid = int.Parse(match.Groups["pid"].Value);
                    if (processPid == pid)
                    {
                        ErrorHandler.ThrowOnFailure(rot.GetObject(moniker[0], out var ideUnk));
                        return new VsIde((DTE)ideUnk, registry, logger);
                    }
                }
            }
            return null;
        }
    }
}
