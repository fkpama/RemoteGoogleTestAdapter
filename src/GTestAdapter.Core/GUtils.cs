using System.Text;
using GoogleTestAdapter.Remote.Models;
using Microsoft.Extensions.Primitives;
using Sodiware.IO;

namespace GoogleTestAdapter.Remote
{
    public static class GUtils
    {
        const string RunningMainFrom = "Running main() from";
        public static string NormalizePath(string filePath)
        {
            return Path.GetFullPath(filePath).ToUpperInvariant();
        }

        public static string GetCommandLineFilter(this IEnumerable<TestCase> testCases)
        {
            var filter = testCases.ToGoogleTestFilter();
            return $"{GoogleTestConstants.FilterOption}{filter}";
        }

        public static string ToGoogleTestFilter(this IEnumerable<TestCase> testCases)
        {
            return string.Join(":", testCases.Select(x => x.DisplayName));
        }

        public static IDictionary<string, List<TestCase>> GroupBySuite(this IEnumerable<TestCase> testcases, string separator = ".")
        {
            var groupedTestCases = new Dictionary<string, List<TestCase>>();
            foreach (TestCase testCase in testcases)
            {
                var idx = testCase.FullyQualifiedName.IndexOf(".");
                var suite = testCase.FullyQualifiedName.Substring(0, idx);
                List<TestCase> group;
                if (groupedTestCases.ContainsKey(suite))
                {
                    group = groupedTestCases[suite];
                }
                else
                {
                    group = new List<TestCase>();
                    groupedTestCases.Add(suite, group);
                }
                group.Add(testCase);
            }
            return groupedTestCases;
        }
        public static IDictionary<string, List<TestCase>> GroupByExecutable(this IEnumerable<TestCase> testcases)
        {
            var groupedTestCases = new Dictionary<string, List<TestCase>>();
            foreach (TestCase testCase in testcases)
            {
                List<TestCase> group;
                if (groupedTestCases.ContainsKey(testCase.Source))
                {
                    group = groupedTestCases[testCase.Source];
                }
                else
                {
                    group = new List<TestCase>();
                    groupedTestCases.Add(testCase.Source, group);
                }
                group.Add(testCase);
            }
            return groupedTestCases;
        }

        internal static ArraySegment<string> Sanitize(string[] testListOutput)
        {
            int start, end = 0;
            for (start = 0; start < testListOutput.Length
                && (string.IsNullOrWhiteSpace(testListOutput[start])
                || testListOutput[start].StartsWith(RunningMainFrom)); start++) ;

            for (var i = testListOutput.Length - 1;
                i >= 0
                && string.IsNullOrWhiteSpace(testListOutput[i]);
                i--, end++) ;

            return new(testListOutput, start, testListOutput.Length - start - end);
        }

        public static bool WaitOne(this WaitHandle handle, TimeSpan span, CancellationToken cancellationToken)
        {
            var ret = WaitHandle
                .WaitAny(new[] { handle, cancellationToken.WaitHandle }, span);
            if (ret == WaitHandle.WaitTimeout)
            {
                return false;
            }
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        public static void WaitOne(this WaitHandle handle, CancellationToken cancellationToken)
        {
            WaitHandle.WaitAny(new[] { handle, cancellationToken.WaitHandle });
            cancellationToken.ThrowIfCancellationRequested();
        }

        internal static string SanitizeBashError(IEnumerable<string> text, string shell = "bash")
        {
            string? prefix = null;
            StringBuilder? sb = null;
            foreach(var line in text)
            {
                prefix ??= $"{shell}: line ";
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var sub = new StringSegment(line, prefix.Length, line.Length - prefix.Length);
                    int i;
                    for (i = 0; i < sub.Length && char.IsDigit(sub[i]); i++) ;
                    Debug.Assert(sub[i] == ':');
                    i++;
                    var toAdd = sub.Subsegment(i).TrimStart();
                    (sb ??= new()).AppendLine(toAdd.Value);
                }
                else
                {
                    (sb ??= new()).AppendLine(line);
                }
            }

            return sb?.ToString() ?? string.Empty;
        }

        public static TestCaseDeploymentProperty? GetDeploymentProperty(this TestCase testCase)
        {
            return testCase.Properties.OfType<TestCaseDeploymentProperty>()
                .SingleOrDefault();
        }

        public static TimeSpan GetRemaining(this TimeSpan timeout, long startTime)
        {
            var ts = Stopwatch.GetTimestamp() - startTime;
            //var remaining = TimeSpan.FromTicks()
            return TimeSpan.FromTicks(timeout.Ticks - ts);
        }

        public static string CompilerToEditorPath(this IEnumerable<SourceMap> maps, string compilerPath)
        {
            compilerPath = PathUtils.Unix.NormalizePath(compilerPath);
            foreach(var map in maps
                .Where(x => x.CompilerPath.IsPresent() && x.EditorPath.IsPresent())
                .OrderByDescending(x => x.CompilerPath.Length))
            {
                var normalized = PathUtils.Unix.NormalizePath(map.CompilerPath);
                var isDirectoryEntry = map.CompilerPath.EndsWith(PathUtils.Unix.UnixDirectorySeparator);
                if (compilerPath.EqualsOrd(normalized))
                {
                    return map.EditorPath;
                }
                else if (isDirectoryEntry && compilerPath.StartsWithO(normalized))
                {
                    var result = compilerPath.Replace(normalized, map.EditorPath);
                    var editorPath = PathUtils.Unix.ConvertUnixPathToWindowsPath(result);
                    return editorPath.Replace("\\\\", "\\");
                }
            }
            return compilerPath;
        }
    }

}
