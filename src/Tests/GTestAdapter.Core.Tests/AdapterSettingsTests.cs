using GoogleTestAdapter.Remote;
using GoogleTestAdapter.Remote.Adapter.Settings;

namespace GTestAdapter.Core.Tests
{
    internal class AdapterSettingsTests
    {
        [Test]
        public void can_deserialize_settings()
        {
            var id = Guid.NewGuid().ToString();
            var model = new AdapterSettingsModel
            {
                RemoteDebuggerPipeId = id
            };
            var format = AdapterSettingsModel.Serialize(model);
            string setting = $"""
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
{format}
</RunSettings>
""";
            var settings = new VsTestFrameworkAdapterSettings(setting);

            settings.DebuggerPipeId.Should().HaveValue();
            settings.DebuggerPipeId!.Value.Should().Be(id);
        }
    }
}
