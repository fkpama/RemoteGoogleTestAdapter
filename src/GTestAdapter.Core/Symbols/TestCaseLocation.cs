using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Remote.Symbols
{
    public class TestCaseLocation : SourceFileLocation
    {
        private readonly string[]? namespaces;

        public IList<Trait> Traits { get; } = new List<Trait>();
        public string[] Namespaces => namespaces ?? Array.Empty<string>();

        public TestCaseLocation(string? sourceFile,
                                uint line,
                                string[]? namespaces)
            : base(string.Empty, sourceFile, line)
        {
            this.namespaces = namespaces;
        }
    }
}
