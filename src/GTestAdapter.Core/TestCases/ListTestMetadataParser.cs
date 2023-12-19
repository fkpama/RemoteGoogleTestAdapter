using GoogleTestAdapter.Remote.Models;

namespace GoogleTestAdapter.Remote.TestCases
{
    internal sealed class ListTestMetadataParser
    {
        public TestListMetadata ParseTestListOutput(IReadOnlyList<string> outputs)
        {
            outputs = GUtils.Sanitize(outputs as string[] ?? outputs.ToArray());
            var nbTestsPerSuite = new Dictionary<string, int>();
            string? currentSuite = null;
            int totalTestsInExe = 0,
                currentSuiteCount = 0;
            for(int i = 0; i < outputs.Count; i++)
            {
                string trimmedLine = outputs[i].Trim('.', '\n', '\r');
                if (trimmedLine.IsMissing()) continue;
                if (trimmedLine.StartsWith("  ", StringComparison.Ordinal))
                {
                    currentSuiteCount++;
                    totalTestsInExe++;
                }
                else
                {
                    addCurrentSuite();
                    currentSuite = trimmedLine;
                    currentSuiteCount = 0;
                }
            }

            addCurrentSuite();
            return new(nbTestsPerSuite, totalTestsInExe);


            void addCurrentSuite()
            {
                if (currentSuite.IsPresent())
                {
                    Debug.Assert(currentSuiteCount > 0);
                    Assumes.NotNull(currentSuite);
                    if (currentSuiteCount > 0)
                    {
                        nbTestsPerSuite[currentSuite] = currentSuiteCount;
                    }
                }
            }
        }
    }
}
