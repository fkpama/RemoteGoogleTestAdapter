using System.Text.RegularExpressions;
using GoogleTestAdapter.Remote.Settings;

namespace GoogleTestAdapter.Remote.Runners
{
    public sealed class PathMapperFilter : ITestOutputFilter
    {
        static readonly Regex s_failureRegex = new(@"(?<path>.+):(?<line>\d+): Failure",
            RegexOptions.Compiled);
        private IReadOnlyList<SourceMap>? maps;

        public PathMapperFilter(IReadOnlyList<SourceMap>? maps)
        {
            this.maps = maps;
        }

        public string Transform(string line)
        {
            if (this.maps is null || this.maps.Count == 0) { return line; }
            var match = s_failureRegex.Match(line);
            if (match.Success)
            {
                var path = match.Groups["path"].Value;
                var replaced = maps.CompilerToEditorPath(path);
                if (!replaced.EqualsOrd(path))
                {
                    return line.Replace(path, replaced);
                }
            }

            return line;
        }
    }
}
