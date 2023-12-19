using System.ComponentModel.Composition;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using ITestWindowLogger = Microsoft.VisualStudio.TestWindow.Extensibility.ILogger;

namespace GoogleTestAdapter.Remote.VsPackage.Debugger
{
    [Export(typeof(IRunSettingsService))]
    public sealed class SettingsListener : IRunSettingsService
    {
        enum ExecutionState
        {
            Started,
            Stopped
        }
        private TestOperationStates kind;
        private readonly ISolutionListener listener;
        private ExecutionState state;

        public string Name { get; } = Resources.SettingsListenerName;

        [ImportingConstructor]
        internal SettingsListener(
            IUnitTestNotifyChanged unitTestNotifyChanged,
            ISolutionListener listener)
        {
            unitTestNotifyChanged.OnUnitTestChanged += onUnitTestChanged;
            this.listener = listener;
        }

        private void onUnitTestChanged(object? sender,
            UnitTestChangedEventArgs args)
        {
            this.kind = args.Kind;
            if (args.Kind == TestOperationStates.TestExecutionStarting)
            {
                this.state = ExecutionState.Started;
            }
            else if (args.Kind == TestOperationStates.TestExecutionFinished)
            {
                this.state = ExecutionState.Stopped;
            }
            else if (args.Kind == TestOperationStates.DiscoveryFinished)
            {

            }
        }

        public IXPathNavigable AddRunSettings(
            IXPathNavigable inputRunSettingDocument,
            IRunSettingsConfigurationInfo configurationInfo,
            ITestWindowLogger log)
        {
            var isExecution = configurationInfo.RequestState == RunSettingConfigurationInfoState.Execution;
            if (this.listener.IsEnabled)
            {
                //bool isDebugRequest = isExecution && (configurationInfo
                //    .TestContainers
                //    ?.Any(x => x.DebugEngines?.Any() == true)
                //    ?? false);
                log.Log(MessageLevel.Informational, "Starting Remote Google adapter tests");
                var settings = new AdapterSettingsModel
                {
                    SourceMap = this.listener.GetSourceMap(),
                    Connections = this.listener.GetConnections(),
                };
                var settingsXml = AdapterSettingsModel.Serialize(settings);

                var nav = inputRunSettingDocument.CreateNavigator();
                nav.MoveToChild(XPathNodeType.Element);
                nav.AppendChild(settingsXml);
            }
            return inputRunSettingDocument;
        }
    }
}