using GoogleTestAdapter.Remote.Adapter.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GTestAdapter.Core.Tests
{
    internal class VsTestFrameworkSettingsTests
    {
        [Test]
        public void can_get_runsettings()
        {
            var sut = new VsTestFrameworkAdapterSettings(TestDatas.RunSettings.ExampleSettings);

            var wrapper =  sut.GetWrapper();
            Assert.That(wrapper, Is.Not.Null);
        }

        [Test]
        public void do_not_crash_if_no_runsettings()
        {
            var sut = new VsTestFrameworkAdapterSettings(TestDatas.RunSettings.EmptySettings);

            var wrapper =  sut.GetWrapper();
            Assert.That(wrapper, Is.Not.Null);
        }
    }
}
