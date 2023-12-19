using GoogleTestAdapter.Remote.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Remote.Adapter.Logging
{
    internal class VsTestFrameworkLoggerFactory : ILoggerFactory
    {
        private readonly IMessageLogger? logger;
        private readonly AdapterSettings settings;

        public void AddProvider(ILoggerProvider provider) { }

        public VsTestFrameworkLoggerFactory(IMessageLogger? logger,
                                            AdapterSettings settings)
        {
            this.logger = logger;
            this.settings = settings;
        }

        public IMSLogger CreateLogger(string categoryName)
        {
            return new VsTestFrameworkLogger(this.logger,
                () => this.settings.DebugMode,
                () => this.settings.TimestampOutput);
        }

        public void Dispose() { }
    }
}