using GoogleTestAdapter.Common;
using GoogleTestAdapter.Remote;
using GoogleTestAdapter.Remote.Adapter.Debugger.ServiceModel;
using GoogleTestAdapter.Remote.Debugger;

namespace GTestAdapter.Core.Tests
{
    internal class DebuggerLauncherTests
    {
        private Thread createService(Guid id, IDebuggerLauncher instance, AutoResetEvent shutdownEvent)
        {
            var logger = new Mock<ILogger>();
            var factory = new DebuggerServiceFactory();
            var barrier = new ManualResetEventSlim(false);
            Exception? raisedEx = null;
            var t = new Thread(() =>
            {
                Console.WriteLine("Opening service");
                try
                {
                    var host = factory.Open(id, instance, logger.Object);
                    Console.WriteLine("Service successfully opened");
                    barrier.Set();
                    shutdownEvent.WaitOne();
                }
                catch(Exception ex)
                {
                    raisedEx = ex;
                }
                finally
                {
                    barrier.Set();
                }
            })
            {
                IsBackground = true
            };
            t.Start();
            var ok  = barrier.Wait(TimeSpan.FromSeconds(2));
            if (raisedEx is not null)
            {
                //var info = ExceptionDispatchInfo.Capture(raisedEx);
                throw raisedEx;
            }
            Assert.That(ok); ;
            return t;
        }
        [Test, Ignore("TODO")]
        public void can_create_launcher()
        {
            var serviceCallBarrier = new Barrier(2);
            var guid = Guid.NewGuid();
            var mock = new Mock<IDebuggerLauncher>();
            mock.Setup(x => x.ReportTestStarted(It.IsAny<string[]>())).Verifiable();
            var evt = new AutoResetEvent(false);
            var t = createService(guid, mock.Object, evt);

            var client = DebuggerClientFactory
                .Create(guid, TimeSpan.FromSeconds(30));
            client.ReportTestStarted(Array.Empty<string>());

            evt.Set();
            mock.Verify();
        }
    }
}
