using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TestCase = GoogleTestAdapter.Model.TestCase;
using TestOutcome = GoogleTestAdapter.Model.TestOutcome;
using TestResult = GoogleTestAdapter.Model.TestResult;
using Trait = GoogleTestAdapter.Model.Trait;
using VsTestProperty = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestProperty;
using TestCaseProperties = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCaseProperties;
using VsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using VsTestOutcome = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome;
using VsTrait = Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using GoogleTestAdapter.Remote.Models;
using GoogleTestAdapter.Remote.Adapter.Utils;
using Microsoft.VisualStudio.PlatformUI;

namespace GoogleTestAdapter.Remote.Adapter
{

    public static class DataConversionExtensions
    {
        private static readonly VsTestProperty TestMetaDataProperty;
        private static readonly VsTestProperty TestDeploymentMetaDataProperty;
        static DataConversionExtensions()
        {
            const TestPropertyAttributes attributes = TestPropertyAttributes.Hidden | TestPropertyAttributes.Immutable;
            TestMetaDataProperty = VsTestProperty
                .Register(TestCaseMetaDataProperty.Id,
                          TestCaseMetaDataProperty.Label,
                          typeof(string),
                          attributes,
                          typeof(VsTestCase));
            TestDeploymentMetaDataProperty = VsTestProperty
                .Register(TestCaseDeploymentProperty.Id,
                          TestCaseDeploymentProperty.Label,
                          typeof(string),
                          attributes,
                          typeof(VsTestCase));
        }

        public static void SetDeploymentProperty(this VsTestCase testCase,
                                               int connectionId,
                                               string? remoteExePath)
        {
            var property = new TestCaseDeploymentProperty(connectionId,
                remoteExePath);
            property.SetValue(TestDeploymentMetaDataProperty, testCase);
        }

        public static void SetMetadataProperty(this VsTestCase testCase,
                                               int nrOfTestCasesInSuite,
                                               int nrOfTestCasesInExecutable,
                                               string fullyQualifiedNameWithoutNamespace)
        {
            var metadatas = new TestCaseMetaDataProperty(nrOfTestCasesInSuite,
                                                         nrOfTestCasesInExecutable,
                                                         fullyQualifiedNameWithoutNamespace);
            metadatas.SetValue(TestMetaDataProperty, testCase);
        }


        public static TestCase ToTestCase(this VsTestCase vsTestCase)
        {
            TestCaseMetaDataProperty? metaData = null;
            var metaDataSerialization = vsTestCase.GetPropertyValue(TestMetaDataProperty);
            if (metaDataSerialization != null)
                metaData = new TestCaseMetaDataProperty((string)metaDataSerialization);

            var fullyQualifiedNameWithoutNamespace = vsTestCase.FullyQualifiedName;
            if (metaData != null)
                fullyQualifiedNameWithoutNamespace = metaData.FullyQualifiedNameWithoutNamespace;

            var testCase = new TestCase(fullyQualifiedNameWithoutNamespace, vsTestCase.FullyQualifiedName, vsTestCase.Source, 
                vsTestCase.DisplayName, vsTestCase.CodeFilePath, vsTestCase.LineNumber);
            testCase.Traits.AddRange(vsTestCase.Traits.Select(ToTrait));

            if (metaData != null)
                testCase.Properties.Add(metaData);

            var deploymentSerialization = vsTestCase.GetPropertyValue<string>(TestDeploymentMetaDataProperty, null);
            if (deploymentSerialization.IsPresent())
            {
                Assumes.NotNull(deploymentSerialization);
                var metadata  = TestCaseDeploymentProperty.Parse(deploymentSerialization);
                testCase.SetDeploymentProperty(metadata);
            }

            return testCase;
        }

        public static void SetDeploymentProperty(this TestCase testCase, TestCaseDeploymentProperty deploymentProperty)
        {
            testCase.Properties.Add(deploymentProperty);
        }

        public static TestCaseDeploymentProperty GetDeploymentProperty(this VsTestCase testCase)
        {
            var str = testCase.GetPropertyValue(TestDeploymentMetaDataProperty);
            if (str is null)
            {
                return null;
            }
            if (str is not string s)
            {
                throw new NotImplementedException();
            }

            return TestCaseDeploymentProperty.Parse(s);
        }

        public static VsTestCase ToVsTestCase(this TestCase testCase, string? overrideSource = null)
        {
            var vsTestCase = new VsTestCase(testCase.FullyQualifiedNameWithNamespace,
                                            new(TestExecutor.ExecutorUri),
                                            overrideSource.IfMissing(testCase.Source))
            {
                DisplayName = testCase.DisplayName,
                CodeFilePath = testCase.CodeFilePath,
                LineNumber = testCase.LineNumber
            };

            //if (testCase.CodeFilePath.IsPresent())
            //{
            //    vsTestCase.SetPropertyValue(TestCaseProperties.CodeFilePath, testCase.CodeFilePath);
            //    vsTestCase.SetPropertyValue(TestCaseProperties.LineNumber, testCase.LineNumber);
            //}

            vsTestCase.Traits.AddRange(testCase.Traits.Select(ToVsTrait));

            testCase.Properties
                .OfType<TestCaseMetaDataProperty>()
                .SingleOrDefault()
                .SetValue(TestMetaDataProperty, vsTestCase);

            vsTestCase.SetHierarchy(testCase.Properties
                .OfType<TestCaseHierarchyProperty>()
                .SingleOrDefault());

            testCase.Properties
                .OfType<TestCaseDeploymentProperty>()
                .SingleOrDefault()
                .SetValue(TestDeploymentMetaDataProperty, vsTestCase);

            return vsTestCase;
        }


        private static Trait ToTrait(this VsTrait trait)
        {
            return new Trait(trait.Name, trait.Value);
        }

        private static VsTrait ToVsTrait(this Trait trait)
        {
            return new VsTrait(trait.Name, trait.Value);
        }


        public static VsTestResult ToVsTestResult(this TestResult testResult)
        {
            return new VsTestResult(ToVsTestCase(testResult.TestCase))
            {
                Outcome = testResult.Outcome.ToVsTestOutcome(),
                ComputerName = testResult.ComputerName,
                DisplayName = testResult.DisplayName,
                Duration = testResult.Duration,
                ErrorMessage = testResult.ErrorMessage,
                ErrorStackTrace = testResult.ErrorStackTrace
            };
        }

        public static TestOutcome ToTestOutcome(this Outcome outcome)
            => outcome switch
            {
                Outcome.Passed => TestOutcome.Passed,
                Outcome.Failed => TestOutcome.Failed,
                Outcome.Skipped => TestOutcome.Skipped,
                Outcome.NotFound => TestOutcome.NotFound,
                Outcome.None => TestOutcome.None,
                _ => throw new InvalidDataException(Resources.UnknownLiteral)
            };

        public static VsTestOutcome ToVsTestOutcome(this TestOutcome testOutcome)
        {
            switch (testOutcome)
            {
                case TestOutcome.Passed:
                    return VsTestOutcome.Passed;
                case TestOutcome.Failed:
                    return VsTestOutcome.Failed;
                case TestOutcome.Skipped:
                    return VsTestOutcome.Skipped;
                case TestOutcome.None:
                    return VsTestOutcome.None;
                case TestOutcome.NotFound:
                    return VsTestOutcome.NotFound;
                default:
                    throw new Exception();
            }
        }

        public static Severity GetSeverity(this TestMessageLevel level) => level switch
        {
            TestMessageLevel.Informational => Severity.Info,
            TestMessageLevel.Warning => Severity.Warning,
            TestMessageLevel.Error => Severity.Error,
            _ => throw new UnreachableException(),
        };

        public static IEnumerable<TestResult> ToTestResult(this IEnumerable<TestCase> testCases,
            TestOutcome outcome,
            string? errorMessage = null)
            => testCases.Select(testCase => new TestResult(testCase)
            {
                Outcome = outcome,
                ErrorMessage = errorMessage
            });
    }

}
