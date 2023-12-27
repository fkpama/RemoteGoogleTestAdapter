using System.Text;

namespace DebugLibrary.Tests.Utils
{
    public readonly struct CompilationUnitResult
    {
        public CompilationUnitRequest Unit { get; init; }
        public string ObjectFilePath { get; init; }
        public string SourcePath { get; init; }
    }

    public readonly struct CompilationResult
    {
        public string OutputFilename { get; init; }
        public IReadOnlyList<CompilationUnitResult> Units { get; init; }
        public required string CompilationDir { get; init; }

        public void ThrowIfFailed() { }
    }
    public enum OutputType
    {
        Application,
        SharedLibrary
    }
    public enum Language
    {
        C99,
        Cxx11,
        Cxx14,
        Cxx17,
        Cxx20,
    }
    public readonly struct CompilationUnitRequest
    {
        public Language? Language { get; init; }
        public required string Name { get; init; }
        public required string Content { get; init; }
        public string? OutputFileName { get; init; }
        public int? DwarfVersion { get; init; }
    }
    struct CompilationRequest
    {
        public Language Language { get; set; }
        public CompilationUnitRequest[] Units { get; set; }
        public int DwarfVersion { get; set; }
        public OutputType OutputType { get; set; }
        public string[] AdditionalLibraryDependencies { get; set; }
        public string[] AdditionalDependencies { get; set; }
    }
    public sealed class WslLinuxRunner
    {
        public const int DefaultDwarfVersion = 2;
        string pwd;
        private string GxxExe = "g++";

        public string Pwd
        {
            get
            {
                pwd ??= LinuxUtil.RunLinuxExe("pwd", print: false).Trim();
                return pwd;
            }
        }

        public WslLinuxRunner()
        {
            pwd = null!;
        }

        public Task<CompilationResult> CompileAsync(CompilationUnitRequest[] units,
                                                    int dwarfVersion = DefaultDwarfVersion,
                                                    Language language = Language.C99,
                                                    OutputType outputType = OutputType.Application,
                                                    string[]? additionalLibraryDependencies = null,
                                                    string[]? additionalDependencies = null,
                                                    string? fileBaseName = null,
                                                    CancellationToken cancellationToken = default)
            => CompileAsync(null,
                            units: units,
                            dwarfVersion: dwarfVersion,
                            language: language,
                            outputType: outputType,
                            additionalLibraryDependencies: additionalLibraryDependencies,
                            additionalDependencies: additionalDependencies,
                            fileBaseName: fileBaseName,
                            cancellationToken: cancellationToken);
        public Task<CompilationResult> CompileAsync(string? outputFileName,
                                                    CompilationUnitRequest[] units,
                                                    int dwarfVersion,
                                                    Language language = Language.C99,
                                                    OutputType outputType = OutputType.Application,
                                                    string[]? additionalLibraryDependencies  = null,
                                                    string[]? additionalDependencies = null,
                                                    string? fileBaseName = null,
                                                    CancellationToken cancellationToken = default)
        {
            var result = new CompilationResult[units.Length];
            //var objectFiles = new List<string>();
            var pwd = Pwd;
            var cmd = new StringBuilder();
            var exe = getExe(language);
            string output;
            var request = new CompilationRequest
            {
                Language = language,
                Units = units,
                DwarfVersion = dwarfVersion,
                AdditionalLibraryDependencies = additionalLibraryDependencies ?? Array.Empty<string>(),
                AdditionalDependencies = additionalDependencies ?? Array.Empty<string>(),
                OutputType = outputType
            };
            var objectFiles = compileObjectFiles(pwd, request, cancellationToken);
            if (objectFiles.Any(x => isCplusplusFilename(x.SourcePath)))
            {
                exe = GxxExe;
            }


            var opath = getOutputFile(outputFileName, () =>
            {
                var testName = fileBaseName;
                if (!string.IsNullOrWhiteSpace(testName))
                {
                    testName += $"_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                }
                else
                {
                    testName = $"{Guid.NewGuid():N}";
                }
                return $"{testName}_dwarf-{dwarfVersion}.out";
            });
            cancellationToken.ThrowIfCancellationRequested();
            var str = string.Join(" ", objectFiles.Select(x => $"\"{x.ObjectFilePath}\""));
            cmd.Clear();
            if (outputType == OutputType.SharedLibrary)
            {
                cmd.Insert(0, "-shared ");
            }
            cmd.Append($"-o {opath}");
            if (outputType == OutputType.SharedLibrary)
            {
                cmd.Append(" -fPIC");
            }
            cmd.Append($" -g -gdwarf-{dwarfVersion}");
            cmd.Append($" -O0 {str}");

            if (request.AdditionalDependencies?.Length > 0)
            {
                cmd.Append($" {string.Join(" ", request.AdditionalDependencies)}");
            }

            if (request.AdditionalLibraryDependencies.Length > 0)
            {
                //cmd.Append(" -L/usr/lib/x86_64-linux-gnu");
                cmd.Append($" {string.Join(" ", request.AdditionalLibraryDependencies
                    .Select(x => $"-l{x}"))}");
            }
            output = LinuxUtil.RunLinuxExe(exe, cmd.ToString());
            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine(output);
            }


            var r = new CompilationResult
            {
                CompilationDir = pwd,
                OutputFilename = opath,
                Units = objectFiles
            };
            return Task.FromResult(r);
        }

        private string languageToCommandLineArg(Language language)
        {
            var val = language switch
            {
                Language.Cxx11 => "c++11",
                Language.Cxx14 => "c++14",
                Language.Cxx17 => "c++17",
                Language.Cxx20 => "c++20",
                Language.C99 => "c99",
                _ => throw new NotSupportedException($"Unknown language {language}")
            };
            return $"-std={val}";
        }

        private string getOutputFile(string? outputFileName, Func<string>? fallback = null)
        {
            if (string.IsNullOrWhiteSpace(outputFileName))
            {
                if (fallback is null)
                    throw new NotImplementedException();
                return fallback();
            }
            if (LinuxUtil.IsAbsolute(outputFileName))
            {
                return outputFileName;
            }
            return mapToRemovePath(outputFileName);
        }

        private string mapToRemovePath(string v)
        {
            // TODO
            return v;
        }
        private static string getExe(Language language)
            => language == Language.C99 ? "gcc" : "g++";

        private List<CompilationUnitResult> compileObjectFiles(string pwd,
            CompilationRequest request,
            CancellationToken cancellationToken)
        {
            var units = request.Units;
            var cmd = new StringBuilder();
            var result = new List<CompilationUnitResult>();
            foreach (var item in units)
            {
                var dwarfVersion = item.DwarfVersion ?? request.DwarfVersion;
                cancellationToken.ThrowIfCancellationRequested();
                cmd.Clear();
                var fpath = getOutputFile(item.Name);
                var lan = item.Language ?? request.Language;
                fpath = ensureExtension(fpath, lan);
                var outputFname = item.OutputFileName;
                if (outputFname != null)
                {
                    outputFname = Path.ChangeExtension(item.Name, ".o");
                }
                else
                {
                    outputFname = $"{item.Name}_{Guid.NewGuid():N}.o";
                }
                cmd.Append($"-c -O0 -o {outputFname}");
                var stdArg = languageToCommandLineArg(lan);
                cmd.Append($" {stdArg}");
                cmd.Append($" -gdwarf-{dwarfVersion}");

                var localPath = Path.Combine(Environment.CurrentDirectory, item.Name);
                localPath = Path.GetDirectoryName(localPath)!;
                localPath = Path.Combine(localPath, LinuxUtil.GetFilename(fpath));
                //objectFiles.Add(outputFname);
                File.WriteAllText(localPath, item.Content);
                result.Add(new()
                {
                    Unit = item,
                    ObjectFilePath = outputFname,
                    SourcePath = localPath
                });
                cmd.Append($" \"{fpath}\"");

                var output = LinuxUtil.RunLinuxExe(getExe(lan), cmd.ToString());
                if (!string.IsNullOrWhiteSpace(output))
                {
                    Console.WriteLine(output);
                }
            }

            return result;
        }

        private static string ensureExtension(string fpath, Language lan)
        {

            fpath = fpath.Trim();
            if (!hasValidExtension(fpath))
            {
                fpath = $"{fpath}{getExtension(lan)}";
            }
            return fpath;
        }

        private static string getExtension(Language lan)
            => isCplusplus(lan) ? ".cpp" : ".c";
        private static bool isCplusplusFilename(string fileName)
            => !string.IsNullOrWhiteSpace(fileName)
            && string.Equals(Path.GetExtension(fileName), ".cpp");
        private static bool isCplusplus(Language lan)
        {
            return lan.ToString().StartsWith("Cxx");
        }

        private static bool hasValidExtension(string fpath)
        {
            var p = fpath.Trim();
            return p.EndsWith(".c") || p.EndsWith(".cpp");
        }
        public string RunCommand(string command, string? arguments = null)
            => LinuxUtil.RunLinuxExe(command, arguments);
    }
}
