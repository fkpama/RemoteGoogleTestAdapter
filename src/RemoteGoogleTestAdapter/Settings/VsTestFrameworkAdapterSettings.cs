using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.XPath;
using GoogleTestAdapter.Remote.Settings;
using GoogleTestAdapter.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Remote.Adapter.Settings
{
    internal sealed partial class VsTestFrameworkAdapterSettings : AdapterSettings
    {
        private readonly ILogger? logger;
        private readonly XmlDocument? document;
        private AdapterSettingsModel? model;
        private bool? m_isRunningInsideVs;
        private bool? m_isBeingDebugged;
        private readonly Lazy<SettingsWrapper> runSettings;

        public VsTestFrameworkAdapterSettings(IRunSettings? settings = null,
                                              ILogger? logger = null)
            : this(settings?.SettingsXml, logger)
        {
        }
        public VsTestFrameworkAdapterSettings(string? settings, ILogger? logger = null)
        {
            if (settings.IsPresent())
            {
                try
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(settings);
                    this.document = doc;
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Error loading document: {ex.Message}");
                }
            }
            this.logger = logger;
            this.runSettings = new(doCreateWrapper);
        }

        public override List<ConnectionId>? Connections => Model?.Connections;
        public override List<SourceMap>? SourceMap => Model?.SourceMap;

        internal AdapterSettingsModel Model
        {
            get
            {
                if (this.model is null)
                {
                    if(this.document is not null)
                    {
                        var ns = AdapterSettingsModel.Namespace;
                        var name = AdapterSettingsModel.ElementName;
                        var node = this.document.SelectSingleNode(name, ns);
                        if (node is not null)
                        {
                            this.model = AdapterSettingsModel.Deserialize(node.OuterXml);
                        }
                    }
                    if (this.model is null)
                        model = new();
                }
                return this.model;
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
        public override TimeSpan TestDiscoveryTimeout
        {
            get
            {
                var timeout = this.runSettings.Value.TestDiscoveryTimeoutInSeconds;
                return timeout == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(timeout);
            }
        }
        public override int NrOfTestRepetitions
        {
            get => this.runSettings.Value.NrOfTestRepetitions;
        }

        public override bool TimestampOutput
        {
            get
            {
                try
                {
                    return this.runSettings.Value.TimestampOutput;
                }
                catch (InvalidOperationException)
                {
                    // Run settings parsing went bad
                    // and we're trying to log it
                    return SettingsWrapper.OptionTimestampOutputDefaultValue;
                }
            }
        }

        public override bool IsBeingDebugged
        {
            get => this.m_isBeingDebugged.HasValue
                && this.m_isBeingDebugged.Value;
        }

        public override bool IsRunningInsideVisualStudio
        {
            get => this.m_isRunningInsideVs.HasValue
                ? this.m_isRunningInsideVs.Value
                : IsVisualStudioBackgroundDiscovery;
        }

        public override bool DebugMode
        {
            get
            {
                var value = this.GetGoogleTestSetting();
                return GetBool(value, "GoogleTest/DebugMode", false);
            }
        }

        private string? GetGoogleTestSetting([CallerMemberName]string? setting = null)
        {
            Assumes.NotNull(setting);
            setting = setting.RemoveLeadingSlash();
            return GetSetting($"/RunSettings/GoogleTestAdapterSettings/SolutionSettings/Settings/{setting}");
        }
        private string? GetSetting(string setting)
        {
            if (this.document is null)
            {
                return null;
            }
            var nav = document.CreateNavigator();
            var node = nav.SelectSingleNode(setting);
            return node?.Value;
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

        public override Guid? DebuggerPipeId
        {
            get
            {
                var id =this.Model?.RemoteDebuggerPipeId;
                return id.IsPresent()
                    && Guid.TryParse(id, out var guid)
                    ? guid
                    : null;
            }
        }

        bool GetBool(string? text, string setting, bool fallback = false)
        {
            bool ret = fallback;
            if (!string.IsNullOrWhiteSpace(text))
            {
                Assumes.NotNull(text);
                try
                {
                    ret = XmlConvert.ToBoolean(text.ToLowerInvariant());
                }
                catch (FormatException)
                {
                    logger?.LogWarning($"Invalid bool value for setting {setting}");
                }
            }
            return ret;
        }

        internal void SetIsRunningInsideVisualStudio()
        {
            this.m_isRunningInsideVs = true;
        }

        internal void SetIsBeingDebugged()
        {
            this.m_isBeingDebugged = true;
        }

        public override SettingsWrapper GetWrapper() => this.runSettings.Value;

        SettingsWrapper doCreateWrapper()
        {
            var settings = createRunSettings();
            return new SettingsWrapper(settings);
            RunSettingsContainer? createRunSettings()
            {
                RunSettingsContainer? result = null;
                var element = this.document?
                .DocumentElement
                .SelectSingleNode(GoogleTestConstants.SettingsName);
                if (element is not null)
                {
                    try
                    {
                        using var reader = new StringReader(element.OuterXml);
                        XPathDocument doc = new(reader);
                        var nav = doc.CreateNavigator();
                        nav.MoveToChild(GoogleTestConstants.SettingsName, string.Empty);
                        result = RunSettingsContainer.LoadFromXml(nav);
                    }
                    catch(InvalidRunSettingsException ex)
                    when(ex.InnerException is not null)
                    {
                        logger?.LogError(ex.InnerException.Message);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, Resources.RunSettingsLoadError, ex.Message);
                    }
                }
                return result ?? new();
            }
        }
    }
}
