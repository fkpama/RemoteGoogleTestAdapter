using ELFSharp.ELF.Sections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sodiware.Unix.DebugLibrary;

namespace GoogleTestAdapter.Remote.Symbols
{
    public class ElfBinaryDiscoverer
    {
        const string GTestInternalNameSpace = "testing::internal::";
        private static IEnumerable<string> GetAllSymbols(string fileName)
        {
            using var fstream = File.OpenRead(fileName);
            var allSyms = GetAllSymbols(fstream);
            return allSyms;
        }
        private static IEnumerable<string> GetAllSymbols(Stream binary)
        {
            var lst = new List<string>();
            if (ELFSharp.ELF.ELFReader.TryLoad(binary, false, out var elf))
            {
                Console.WriteLine(elf.Type);
                foreach (var section in elf.Sections.OfType<ISymbolTable>())
                {
                    foreach (var entry in section.Entries)
                    {
                        try
                        {
                            var result = CxxDemangler.CxxDemangler.Demangle(entry.Name);
                            lst.Add(result);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception demangling: {entry.Name}: {ex.Message}");
                        }
                    }
                }
            }
            return lst;
        }
        public static bool IsGTestBinary(string source,
                                         [NotNullWhen(true)] out ElfDebugBinary? binary)
            => IsGTestBinary(source, NullLoggerFactory.Instance, out binary);
        public static bool IsGTestBinary(string source,
                                         ILoggerFactory loggerFactory,
                                         [NotNullWhen(true)] out ElfDebugBinary? binary)
        {
            binary = null;
            if (ElfDebugBinary.TryOpen(source, loggerFactory, out binary))
            {
                Assumes.NotNull(binary);
                if (!binary.GetSymbolNames().Any(x => IsTestInfoEntry(x, out _)))
                {
                    binary.Close();
                    binary = null;
                }
            }
            return binary is not null;
        }

        private static bool IsSuiteDummy(string entry, [NotNullWhen(true)] out string? suite)
        {
            const string symName = ">::dummy_";
            const string typeIdHelper = "TypeIdHelper<";
            const string start = GTestInternalNameSpace + typeIdHelper;
            var len = GTestInternalNameSpace.Length + typeIdHelper.Length + symName.Length;
            if (entry.EndsWith(symName, StringComparison.Ordinal)
                && entry.StartsWith(start, StringComparison.Ordinal))
            {
                suite = entry.Substring(start.Length, entry.Length - len);
                if (suite.StartsWith(GTestInternalNameSpace, StringComparison.Ordinal)
                    || string.Equals(suite, "testing::Test", StringComparison.Ordinal))
                {
                    suite = null;
                }
            }
            else
            {
                suite = null;
            }
            return suite != null;
        }

        internal static bool IsTestInfoEntry(string entry, [NotNullWhen(true)] out string? name)
        {
            const string ti = "::test_info_";
            if (entry.EndsWith(ti, StringComparison.Ordinal))
            {
                name = entry.Substring(0, entry.Length - ti.Length);
            }
            else
            {
                name = null;
            }
            return name != null;
        }
    }
}

