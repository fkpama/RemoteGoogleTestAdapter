using System.Runtime.InteropServices;
using System.Text;

namespace DebugLibrary.Tests.Utils
{
    public static class LinuxUtil
    {
        public static string ReadDebugInfos(string file)
        {
            var str1 = RunLinuxExe("readelf", $"{file} --debug-dump=info", print: false);
            var str2 = RunLinuxExe("readelf", $"{file} --debug-dump=abbrev", print: false);
            var str3 = RunLinuxExe("readelf", $"{file} --debug-dump=rawline", print: false);
            return $" --- Section .debug_infos\n\n{str1}\n\n\n --- Section .debug_abbrev\n{str2}\n\n\n --- Section .debug_line\n\n{str3}";
        }

        public static string ReadElf(string file, string arguments = "-W -a")
        {
            return RunLinuxExe("readelf", $"{file} {arguments}");
        }

        public static string RunLinuxExe(string exe, string? arguments = null, string? distribution = null, bool print = true)
        {
            if (exe == null) throw new ArgumentNullException(nameof(exe));
            //if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            //if (distribution == null) throw new ArgumentNullException(nameof(distribution));

            // redirect to a file the output as there is a bug reading back stdout with WSL
            var wslOut = $"wsl_stdout_{Guid.NewGuid()}.txt";

            try
            {
                return doRun(wslOut,
                             exe,
                             arguments,
                             distribution,
                             print);
            }
            finally
            {
                if (File.Exists(wslOut))
                {
                    try
                    {
                        File.Delete(wslOut);
                    }
                    catch (IOException)
                    {
                        Console.WriteLine($"Failed to delete {wslOut}");
                    }
                }
            }
        }

        private static string doRun(string wslOut,
                                    string exe,
                                    string? arguments,
                                    string? distribution,
                                    bool print)
        {

            var commandToPrint = new StringBuilder();
            var exeToPrint = exe;

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            arguments ??= string.Empty;
            if (isWindows)
            {
                var sb = new StringBuilder(arguments);
                if (distribution != null)
                {
                    sb.Insert(0, $"-d {distribution} ");
                }
                commandToPrint.Append(sb.ToString());
                sb.Insert(0, $"{exe} ");
                sb.Append($" > {wslOut}");
                //arguments = $"-d {distribution} {exe} {arguments} > {wslOut}";
                arguments = sb.ToString();
                exe = "wsl.exe";
            }

            StringBuilder? errorBuilder = null;
            StringBuilder outputBuilder = new();

            if (print)
                Console.WriteLine($"Running command {exeToPrint} {commandToPrint}");
            using (var process = new Process()
            {
                StartInfo = new ProcessStartInfo(exe, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = !isWindows,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                },
            })
            {

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (errorBuilder == null)
                    {
                        errorBuilder = new StringBuilder();
                    }

                    errorBuilder.Append(args.Data).Append('\n');
                };

                if (!isWindows)
                {
                    process.OutputDataReceived += (sender, args) => { outputBuilder.Append(args.Data).Append('\n'); };
                }

                process.Start();
                process.BeginErrorReadLine();

                if (!isWindows)
                {
                    process.BeginOutputReadLine();
                }

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Error while running command `{exeToPrint} {commandToPrint}`: {errorBuilder}");
                }

                if (isWindows)
                {
                    var generated = Path.Combine(Environment.CurrentDirectory, wslOut);
                    var result = File.ReadAllText(generated);
                    try
                    {
                        File.Delete(generated);
                    }
                    catch
                    {
                        // ignore
                    }

                    return result;
                }
            }

            return outputBuilder.ToString();
        }

        internal static bool IsAbsolute(string outputFileName) => outputFileName.Trim().StartsWith("/");
        internal static string GetFilename(string outputFileName)
        {
            var o = outputFileName.Trim();
            var idx = o.LastIndexOf("/");
            if (idx < 0)
                return o;
            return o.Substring(idx + 1);
        }
    }
}
