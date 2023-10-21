using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using GoogleTestAdapter.Common;
using GTestAdapter.Core.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sodiware;

namespace RemoteGoogleTestAdapter.Settings
{
    internal sealed class VsTestFrameworkAdapterSettings : AdapterSettings
    {
        private readonly IRunSettings? settings;
        private readonly ILogger logger;
        private XmlDocument? document;

        public VsTestFrameworkAdapterSettings(IRunSettings? settings, ILogger logger)
        {
            this.settings = settings;
            this.logger = logger;
        }

        internal XmlDocument Document
        {
            get
            {
                if (document is null)
                {
                    document = new XmlDocument();
                    if (this.settings is not null)
                    {
                        document.LoadXml(this.settings.SettingsXml);
                    }
                }
                return this.document;
            }
        }

        public override bool CollectSourceInformation
        {
            get
            {
                const string setting = "/RunSettings/RunConfiguration/CollectSourceInformation";
                var value = GetSetting(setting);
                return GetBool(value, "RunConfiguration/CollectSourceInformation", false);
            }
        }

        public bool TimestampOutput
        {
            get
            {
                var value = this.GetGoogleTestSetting();
                return GetBool(value, "GoogleTest/TimestampOutput", false);
            }
        }
        public bool DebugMode
        {
            get
            {
                var value = this.GetGoogleTestSetting();
                return GetBool(value, "GoogleTest/DebugMode", false);
            }
        }

        private string GetGoogleTestSetting([CallerMemberName]string? setting = null)
        {
            Assumes.NotNull(setting);
            setting = setting.RemoveLeadingSlash();
            return GetSetting($"/RunSettings/GoogleTestAdapterSettings/SolutionSettings/Settings/{setting}");
        }
        private string GetSetting(string setting)
        {
            var nav = Document.CreateNavigator();
            var node = nav.SelectSingleNode(setting);
            return node.Value;
        }

        public bool IsVisualStudioBackgroundDiscovery
        {
            get
            {
                return GetBool(Environment.GetEnvironmentVariable("VSTEST_BACKGROUND_DISCOVERY"),
                    "VSTEST_BACKGROUND_DISCOVERY");
            }
        }

        public override string? OverrideSource
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("GTEST_OVERRIDE_SOURCE");
                if (!string.IsNullOrWhiteSpace(env))
                {
                    if (!Path.IsPathRooted(env))
                    {
                        var dir = Path.GetDirectoryName(MethodBase.GetCurrentMethod()
                            .DeclaringType
                            .Assembly
                            .Location);
                        env = Path.Combine(dir, env);
                    }
                    env = Path.GetFullPath(env);
                }
                return env;
            }
        }

        bool GetBool(string text, string setting, bool fallback = false)
        {
            bool ret = fallback;
            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    ret = XmlConvert.ToBoolean(text.ToLowerInvariant());
                }
                catch (FormatException)
                {
                    logger.LogWarning($"Invalid bool value for setting {setting}");
                }
            }
            return ret;
        }
    }
}
