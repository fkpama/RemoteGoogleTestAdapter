using GoogleTestAdapter.Remote.Remoting;


namespace GoogleTestAdapter.Remote.Execution
{
    public abstract class ProcessExecutorProvider : IProcessExecutorProvider
    {
        protected ISourceDeployment Deployment { get; private set; }
        protected ILogger Log { get; private set; }
        protected ISshClientRegistry Connections { get; private set; }

        public ProcessExecutorProvider(ISourceDeployment deployment,
                                       ISshClientRegistry sshClientRegistry,
                                       ILogger logger)
        {
            this.Deployment = deployment;
            this.Connections = sshClientRegistry;
            this.Log = logger;
        }

        public async Task<IProcessExecutor> GetExecutorAsync(TestCase testCase,
                                            CancellationToken cancellationToken)
        {
            var source = testCase.Source;
            var client = await this.Connections
                .GetClientAsync(source, cancellationToken)
                .ConfigureAwait(false);

            return new RemoteProcessExecutor(this.Deployment,
                                             client,
                                             this.Log,
                                             cancellationToken);
        }
    }
}
