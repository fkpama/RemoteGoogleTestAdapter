﻿// This file has been modified by Microsoft on 8/2017.

using System;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TestCase = GoogleTestAdapter.Model.TestCase;
using TestOutcome = GoogleTestAdapter.Model.TestOutcome;
using TestResult = GoogleTestAdapter.Model.TestResult;
using Trait = GoogleTestAdapter.Model.Trait;
using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
using VsTestProperty = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestProperty;
using VsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using VsTestOutcome = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome;
using VsTrait = Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait;
using RemoteGoogleTestAdapter;

namespace GoogleTestAdapter.TestAdapter
{

    public static class DataConversionExtensions
    {
        private static readonly VsTestProperty TestMetaDataProperty;

        static DataConversionExtensions()
        {
            TestMetaDataProperty = VsTestProperty.Register(TestCaseMetaDataProperty.Id, TestCaseMetaDataProperty.Label, typeof(string), typeof(VsTestCase));
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

            return testCase;
        }

        public static VsTestCase ToVsTestCase(this TestCase testCase, string? overrideSource = null)
        {
            var vsTestCase = new VsTestCase(testCase.FullyQualifiedNameWithNamespace, new(TestExecutor.ExecutorUri), overrideSource ?? testCase.Source)
            {
                DisplayName = testCase.DisplayName,
                CodeFilePath = testCase.CodeFilePath,
                LineNumber = testCase.LineNumber
            };

            vsTestCase.Traits.AddRange(testCase.Traits.Select(ToVsTrait));

            var metaData = testCase.Properties.OfType<TestCaseMetaDataProperty>().SingleOrDefault();
            if (metaData != null)
                vsTestCase.SetPropertyValue(TestMetaDataProperty, metaData.Serialization);

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

    }

}
