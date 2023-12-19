namespace GTestAdapter.Core.Tests
{
    internal static class TestDatas
    {
        internal class TestCaseOutput
        {
            public required string OutputAsText { get; init; }
            public string[] Output
            {
                get => this.OutputAsText.Split('\n', '\n');
            }
            public required int TotalTests { get; init; }
            public required Dictionary<string, int> NbTestsPerSuite { get; init; }
        }

        public static readonly TestCaseOutput TestOutput1 = new()
        {
            TotalTests = 5,
            NbTestsPerSuite = new()
            {
                { "ExampleTestSuite", 1 },
                { "ExampleU_TestSuite", 1 },
                { "NestedNamespaceExampleSuite", 1 },
                { "NestedNamespaceExampleFixture", 2 }
            },
            OutputAsText = @"Running main() from /build/googletest-YnT0O3/googletest-1.10.0.20201025/googletest/src/gtest_main.cc
ExampleTestSuite.
  suite_example_test1
ExampleU_TestSuite.
  suite_example_test2
NestedNamespaceExampleSuite.
  nested_suite_example1
NestedNamespaceExampleFixture.
  nested_fixture_example1
  nested_fixture_example2"
        };


        public static class RunSettings
        {
            public const string EmptySettings = """""
<RunSettings>
</RunSettings>
""""";
            public const string ExampleSettings = """""
<RunSettings>
  <GoogleTestAdapterSettings xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    <SolutionSettings>
      <Settings>
        <PrintTestOutput>true</PrintTestOutput>
        <TestDiscoveryRegex />
        <TestDiscoveryTimeoutInSeconds>30</TestDiscoveryTimeoutInSeconds>
        <WorkingDir>$(ExecutableDir)</WorkingDir>
        <PathExtension />
        <CatchExceptions>true</CatchExceptions>
        <BreakOnFailure>false</BreakOnFailure>
        <RunDisabledTests>false</RunDisabledTests>
        <NrOfTestRepetitions>1</NrOfTestRepetitions>
        <ShuffleTests>false</ShuffleTests>
        <ShuffleTestsSeed>0</ShuffleTestsSeed>
        <TraitsRegexesBefore />
        <TraitsRegexesAfter />
        <TestNameSeparator />
        <DebugMode>true</DebugMode>
        <TimestampOutput>false</TimestampOutput>
        <ShowReleaseNotes>false</ShowReleaseNotes>
        <ParseSymbolInformation>true</ParseSymbolInformation>
        <AdditionalTestDiscoveryParam />
        <AdditionalTestExecutionParam />
        <ParallelTestExecution>false</ParallelTestExecution>
        <MaxNrOfThreads>0</MaxNrOfThreads>
        <BatchForTestSetup />
        <BatchForTestTeardown />
        <KillProcessesOnCancel>true</KillProcessesOnCancel>
        <UseNewTestExecutionFramework>true</UseNewTestExecutionFramework>
        <DebuggingNamedPipeId>51ecfcc2-d38f-44cf-bfef-9c14c98dfff4</DebuggingNamedPipeId>
      </Settings>
    </SolutionSettings>
    <ProjectSettings />
  </GoogleTestAdapterSettings>
</RunSettings>
""""";
        }
    }
}
