using System.ServiceModel;
using System.ServiceModel.Channels;

namespace GoogleTestAdapter.Remote.Debugger
{

    public sealed class DebuggerClientFactory
    {
        //private readonly AdapterSettings settings;
        //private readonly ILogger log;

        public DebuggerClientFactory()
        {
            //this.log = logger;
        }

        //int IDebuggedProcessLauncher.LaunchProcessWithDebuggerAttached(
        //    string command,
        //    string workingDirectory,
        //    IDictionary<string, string> additionalEnvVars,
        //    string param,
        //    string pathExtension)
        //{
        //    Assumes.NotNull(this.settings.DebuggerPipeId);
        //    if (!this.settings.DebuggerPipeId.HasValue)
        //    {
        //        log.LogError("Invalid launch of debugger process with");
        //        return -1;
        //    }

        //    var launcher = Create(this.settings.DebuggerPipeId.Value,
        //                          TimeSpan.FromSeconds(30));

        //    launcher.SayHello();
        //    return 0;
        //}

        public static IDebuggerLauncher Create(Guid guid, TimeSpan timeout)
        {
            var address = DebuggerProxyUtils.InterfaceAddress;
            var uri = DebuggerProxyUtils.ConstructPipeUri(guid);
            var endpointUri = new Uri(uri, address);
            var endpointAddress = new EndpointAddress(endpointUri);
#if NETFRAMEWORK
            var binding = new NetNamedPipeBinding()
            {
                OpenTimeout = timeout,
                CloseTimeout = timeout,
                SendTimeout = timeout,
                ReceiveTimeout = timeout
            };
            return ChannelFactory<IDebuggerLauncher>
                .CreateChannel(binding, endpointAddress);
#else
            throw new NotImplementedException();
#endif

        }

    }
}
