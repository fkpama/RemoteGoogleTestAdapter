using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleTestAdapter.Remote.VsPackage.Debugger
{
    public abstract class DebugLaunchProviderBase
    {
        private readonly IAsyncServiceProvider services;

        public class LaunchResult
        {
            public DateTime CreationTime { get; private set; }

            public int ProcessId { get; private set; }

            internal LaunchResult(DateTime creationTime, int processId)
            {
                CreationTime = creationTime;
                ProcessId = processId;
            }
        }

        protected DebugLaunchProviderBase(IAsyncServiceProvider services)
        {
            this.services = services;
        }

        protected DebugLaunchProviderBase()
            : this(AsyncServiceProvider.GlobalProvider)
        {
        }

        //public virtual async Task<int> QueryDebugTargetsCountAsync(DebugLaunchOptions launchOptions)
        //{
        //    return (await QueryDebugTargetsAsync(launchOptions)).Count;
        //}

        public abstract Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions);

        public abstract Task<bool> CanLaunchAsync(DebugLaunchOptions launchOptions);

        public virtual async Task LaunchAsync(DebugLaunchOptions launchOptions)
        {
            await LaunchAsync((await QueryDebugTargetsAsync(launchOptions)).ToArray());
        }

        internal static VsDebugTargetInfo2 GetDebuggerStruct(IDebugLaunchSettings info)
        {
            Guard.Debug.NotNull(info);
            IList<Guid> debuggerGuids = GetDebuggerGuids(info);
            VsDebugTargetInfo2 vsDebugTargetInfo = default(VsDebugTargetInfo2);
            vsDebugTargetInfo.dlo = (uint)info.LaunchOperation;
            vsDebugTargetInfo.LaunchFlags = (uint)info.LaunchOptions;
            vsDebugTargetInfo.bstrRemoteMachine = info.RemoteMachine;
            vsDebugTargetInfo.bstrArg = info.Arguments;
            vsDebugTargetInfo.bstrCurDir = info.CurrentDirectory;
            vsDebugTargetInfo.bstrExe = info.Executable;
            vsDebugTargetInfo.bstrEnv = GetSerializedEnvironmentString(info.Environment);
            vsDebugTargetInfo.guidLaunchDebugEngine = info.LaunchDebugEngineGuid;
            vsDebugTargetInfo.dwDebugEngineCount = (uint)debuggerGuids.Count;
            vsDebugTargetInfo.pDebugEngines = GetDebugEngineBytes(debuggerGuids);
            vsDebugTargetInfo.guidPortSupplier = info.PortSupplierGuid;
            vsDebugTargetInfo.bstrPortName = info.PortName;
            vsDebugTargetInfo.bstrOptions = info.Options;
            vsDebugTargetInfo.fSendToOutputWindow = info.SendToOutputWindow;
            vsDebugTargetInfo.dwProcessId = (uint)info.ProcessId;
            vsDebugTargetInfo.pUnknown = info.Unknown;
            vsDebugTargetInfo.guidProcessLanguage = info.ProcessLanguageGuid;
            VsDebugTargetInfo2 result = vsDebugTargetInfo;
            result.hStdInput = info.StandardInputHandle;
            result.hStdOutput = info.StandardOutputHandle;
            result.hStdError = info.StandardErrorHandle;
            result.cbSize = (uint)Marshal.SizeOf(typeof(VsDebugTargetInfo2));
            result.dwReserved = 0u;
            return result;
        }

        internal static VsDebugTargetInfo4 GetDebuggerStruct4(IDebugLaunchSettings info)
        {
            Guard.Debug.NotNull(info);
            IList<Guid> debuggerGuids = GetDebuggerGuids(info);
            VsDebugTargetInfo4 vsDebugTargetInfo = default(VsDebugTargetInfo4);
            vsDebugTargetInfo.dlo = (uint)info.LaunchOperation;
            vsDebugTargetInfo.LaunchFlags = (uint)info.LaunchOptions;
            vsDebugTargetInfo.bstrRemoteMachine = info.RemoteMachine;
            vsDebugTargetInfo.bstrArg = info.Arguments;
            vsDebugTargetInfo.bstrCurDir = info.CurrentDirectory;
            vsDebugTargetInfo.bstrExe = info.Executable;
            vsDebugTargetInfo.bstrEnv = GetSerializedEnvironmentString(info.Environment);
            vsDebugTargetInfo.guidLaunchDebugEngine = info.LaunchDebugEngineGuid;
            vsDebugTargetInfo.dwDebugEngineCount = (uint)debuggerGuids.Count;
            vsDebugTargetInfo.pDebugEngines = GetDebugEngineBytes(debuggerGuids);
            vsDebugTargetInfo.guidPortSupplier = info.PortSupplierGuid;
            vsDebugTargetInfo.bstrPortName = info.PortName;
            vsDebugTargetInfo.bstrOptions = info.Options;
            vsDebugTargetInfo.fSendToOutputWindow = info.SendToOutputWindow;
            vsDebugTargetInfo.dwProcessId = (uint)info.ProcessId;
            vsDebugTargetInfo.pUnknown = info.Unknown;
            vsDebugTargetInfo.guidProcessLanguage = info.ProcessLanguageGuid;
            VsDebugTargetInfo4 result = vsDebugTargetInfo;
            if (info.StandardErrorHandle != IntPtr.Zero || info.StandardInputHandle != IntPtr.Zero || info.StandardOutputHandle != IntPtr.Zero)
            {
                VsDebugStartupInfo vsDebugStartupInfo = default(VsDebugStartupInfo);
                vsDebugStartupInfo.hStdInput = info.StandardInputHandle;
                vsDebugStartupInfo.hStdOutput = info.StandardOutputHandle;
                vsDebugStartupInfo.hStdError = info.StandardErrorHandle;
                vsDebugStartupInfo.flags = 256u;
                VsDebugStartupInfo structure = vsDebugStartupInfo;
                result.pStartupInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(structure));
                Marshal.StructureToPtr(structure, result.pStartupInfo, fDeleteOld: false);
            }

            result.AppPackageLaunchInfo = info.AppPackageLaunchInfo;
            result.project = info.Project;
            return result;
        }

        internal static void FreeDebuggerStruct(VsDebugTargetInfo2 nativeStruct)
        {
            if (nativeStruct.pDebugEngines != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(nativeStruct.pDebugEngines);
            }
        }

        internal static void FreeDebuggerStruct(VsDebugTargetInfo4 nativeStruct)
        {
            if (nativeStruct.pDebugEngines != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(nativeStruct.pDebugEngines);
            }

            if (nativeStruct.pStartupInfo != IntPtr.Zero)
            {
                Marshal.DestroyStructure(nativeStruct.pStartupInfo, typeof(VsDebugStartupInfo));
                Marshal.FreeCoTaskMem(nativeStruct.pStartupInfo);
            }
        }

        internal static void CopyStructArrayToIntPtr<T>(T[] list, IntPtr nativeArrayPointer, ref int initializedStructures)
        {
            Guard.Debug.NotNull(list);
            int offset = Marshal.SizeOf(typeof(T));
            for (int i = 0; i < list.Length; i++)
            {
                Marshal.StructureToPtr(list[i], nativeArrayPointer, fDeleteOld: false);
                initializedStructures++;
                nativeArrayPointer = IntPtr.Add(nativeArrayPointer, offset);
            }
        }

        internal static void DestroyStructArray<T>(IntPtr structArray, int arrayLength)
        {
            Guard.Debug.Argument(structArray != IntPtr.Zero, "Null pointer not allowed.");
            Guard.Debug.Range(arrayLength >= 0);
            int offset = Marshal.SizeOf(typeof(T));
            IntPtr intPtr = structArray;
            for (int i = 0; i < arrayLength; i++)
            {
                Marshal.DestroyStructure(intPtr, typeof(T));
                intPtr = IntPtr.Add(intPtr, offset);
            }
        }

        protected async Task<IReadOnlyList<LaunchResult>> LaunchAsync(params IDebugLaunchSettings[] launchSettings)
        {
            Guard.Debug.NotNull(launchSettings);
            VsDebugTargetInfo4[] array = launchSettings.Select(GetDebuggerStruct4).ToArray();
            if (array.Length == 0)
            {
                return (IReadOnlyList<LaunchResult>)Array.Empty<LaunchResult>();
            }

            try
            {
                var obj = await this.services
                    .GetServiceAsync<SVsShellDebugger , IVsDebugger4>()
                    .ConfigureAwait(false);
                Assumes.NotNull(obj);
                VsDebugTargetProcessInfo[] array2 = new VsDebugTargetProcessInfo[array.Length];
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                obj.LaunchDebugTargets4((uint)array.Length, array, array2);
                return array2.Select(GetLaunchResult).ToArray();
            }
            finally
            {
                VsDebugTargetInfo4[] array3 = array;
                for (int i = 0; i < array3.Length; i++)
                {
                    FreeDebuggerStruct(array3[i]);
                }
            }
        }

        private static LaunchResult GetLaunchResult(VsDebugTargetProcessInfo processInfo)
        {
            DateTime creationTime = DateTime.FromFileTime((long)(((ulong)processInfo.creationTime.dwHighDateTime << 32) + processInfo.creationTime.dwLowDateTime));
            int dwProcessId = (int)processInfo.dwProcessId;
            return new LaunchResult(creationTime, dwProcessId);
        }

        private static string? GetSerializedEnvironmentString(IDictionary<string, string> environment)
        {
            if (environment == null || environment.Count == 0)
            {
                return null;
            }

            var stringBuilder = new StringBuilder();
            foreach (var item in environment)
            {
                stringBuilder.Append(item.Key);
                stringBuilder.Append('=');
                stringBuilder.Append(item.Value);
                stringBuilder.Append('\0');
            }

            stringBuilder.Append('\0');
            return stringBuilder.ToString();
        }

        private static byte[] GetGuidBytes(IList<Guid> guids)
        {
            Guard.Debug.NotNull(guids);
            int num = Guid.Empty.ToByteArray().Length;
            byte[] array = new byte[guids.Count * num];
            for (int i = 0; i < guids.Count; i++)
            {
                guids[i].ToByteArray().CopyTo(array, i * num);
            }

            return array;
        }

        private static IntPtr GetDebugEngineBytes(IList<Guid> guids)
        {
            byte[] guidBytes = GetGuidBytes(guids);
            IntPtr intPtr = Marshal.AllocCoTaskMem(guidBytes.Length);
            Marshal.Copy(guidBytes, 0, intPtr, guidBytes.Length);
            return intPtr;
        }

        private static IList<Guid> GetDebuggerGuids(IDebugLaunchSettings info)
        {
            List<Guid> list = new List<Guid>(1 + (info.AdditionalDebugEngines?.Count ?? 0)) { info.LaunchDebugEngineGuid };
            if (info.AdditionalDebugEngines != null)
            {
                list.AddRange(info.AdditionalDebugEngines);
            }

            return list;
        }
    }

}
