using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Remote.Adapter.Traits
{
    // see: https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=mstest
    internal class TestCaseFilter
    {
        private readonly IDictionary<string, TestProperty> _testPropertiesMap = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, TestProperty> _traitPropertiesMap = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);

        private readonly ISet<string> _traitPropertyNames;
        private readonly ISet<string> _allPropertyNames;
        private readonly IRunContext runContext;
        private readonly ILogger logger;

        public TestCaseFilter(IRunContext runContext,
                              IEnumerable<VsTestCase> testCases,
                              ILogger logger)
        {
            this.initialize(testCases);
            this.runContext = runContext;
            this.logger = logger;
        }

        internal IEnumerable<TestCase> Filter(IEnumerable<TestCase> tests)
        {
            var filter = this.runContext
                .GetTestCaseFilter(_allPropertyNames, propertyProvider);

            if (filter is null)
            {
                return tests;
            }

            var result = tests
                .Where(x => filter
                .MatchTestCase(x, str => propertyValueCallback(x, str)));
            return result;
        }

        private object? propertyValueCallback(TestCase tc, string property)
        {
            var testProperty = propertyProvider(property);
            if (testProperty is null)
                return null;

            if (tc.Properties.Contains(testProperty))
                return tc.GetPropertyValue(testProperty);

            //if (_traitPropertyNames.Contains(propertyName))
            //    return GetTraitValues(currentTest, propertyName);

            return null;
        }

        private TestProperty? propertyProvider(string propertyName)
        {
            _testPropertiesMap.TryGetValue(propertyName, out var testProperty);

            if (testProperty is null)
                _traitPropertiesMap.TryGetValue(propertyName, out testProperty);

            return testProperty;
        }

        private void initialize(IEnumerable<VsTestCase> testCases)
        {
            _testPropertiesMap[nameof(TestCaseProperties.FullyQualifiedName)] = TestCaseProperties.FullyQualifiedName;
            _testPropertiesMap[nameof(TestCaseProperties.DisplayName)] = TestCaseProperties.DisplayName;
            _testPropertiesMap[nameof(TestCaseProperties.LineNumber)] = TestCaseProperties.LineNumber;
            _testPropertiesMap[nameof(TestCaseProperties.CodeFilePath)] = TestCaseProperties.CodeFilePath;
            _testPropertiesMap[nameof(TestCaseProperties.ExecutorUri)] = TestCaseProperties.ExecutorUri;
            _testPropertiesMap[nameof(TestCaseProperties.Id)] = TestCaseProperties.Id;
            _testPropertiesMap[nameof(TestCaseProperties.Source)] = TestCaseProperties.Source;

            foreach (string traitName in getAllTraitNames(testCases))
            {
                if (_testPropertiesMap.Keys.Contains(traitName))
                {
                    logger.LogWarning(Resources.TraitIgnoreMessage, traitName);
                    continue;
                }

                var traitTestProperty = TestProperty.Find(traitName) ??
                      TestProperty.Register(traitName,
                                            traitName,
                                            "",
                                            "",
                                            typeof(string),
                                            validateTraitValue,
                                            TestPropertyAttributes.None,
                                            typeof(TestCase));
                _traitPropertiesMap[traitName] = traitTestProperty;
            }
        }

        private bool validateTraitValue(object? value)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<string> getAllTraitNames(IEnumerable<VsTestCase> testCases)
        {
            var allTraitNames = new HashSet<string>();
            foreach (TestCase testCase in testCases)
            {
                foreach (Trait trait in testCase.Traits)
                {
                    allTraitNames.Add(trait.Name);
                }
            }
            return allTraitNames;
        }
    }
}
