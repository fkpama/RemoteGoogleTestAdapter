using GoogleTestAdapter.Remote.Settings;
using Sodiware.IO;

namespace GoogleTestAdapter.Remote.Remoting
{
    public interface ISshClientConnection
    {
        Task<ISshClient> GetClientAsync(CancellationToken cancellationToken);
    }
    public interface ISshClientRegistry
    {
        Task<ISshClient> GetClientAsync(string source, CancellationToken cancellationToken);
        Task<int> GetConnectionIdAsync(string source, CancellationToken cancellationToken);
        void Register(string filePath, ISshClientConnection client);
    }
    sealed class SshClientConnection : ISshClientConnection
    {
        private readonly int connectionId;
        private readonly Func<int, CancellationToken, Task<ISshClient>> factory;
        private ISshClient? client;

        public SshClientConnection(int connectionId, Func<int, CancellationToken, Task<ISshClient>> factory)
        {
            this.connectionId = connectionId;
            this.factory = factory;
        }
        public Task<ISshClient> GetClientAsync(CancellationToken cancellationToken)
        {
            if (this.client is not null)
            {
                return Task.FromResult(this.client);
            }

            return this.factory(this.connectionId, cancellationToken)
                .ContinueWith(t =>
                {
                    var ret = t.RethrowOrGetResult();
                    this.client = ret;
                    return ret;
                });
        }
    }
    public sealed class SshClientRegistry : ISshClientRegistry
    {
        private readonly Dictionary<string, ISshClientConnection> connections = new();
        private ISshClient? defaultConnection;
        private readonly CancellationToken adapterShutdownToken;
        private readonly AdapterSettings settings;
        private readonly Func<int, CancellationToken, Task<ISshClient>> connectionFactory;
        private readonly Func<CancellationToken, Task<ISshClient>> defaultConnectionFactory;

        public SshClientRegistry(CancellationToken adapterShutdownToken,
                                 AdapterSettings settings,
                                 Func<int, CancellationToken, Task<ISshClient>> connectionFactory,
                                 Func<CancellationToken, Task<ISshClient>> defaultConnection)
        {
            this.adapterShutdownToken = adapterShutdownToken;
            this.settings = settings;
            this.connectionFactory = connectionFactory;
            this.defaultConnectionFactory = defaultConnection;
        }

        public void Register(string filePath, ISshClientConnection client)
        {
            var path = GUtils.NormalizePath(filePath);
            this.connections.Add(path, client);
        }

        public async Task<int> GetConnectionIdAsync(string source, CancellationToken cancellationToken)
        {
            var client = await GetClientAsync(source, cancellationToken).ConfigureAwait(false);
            return client.ConnectionId;
        }
        public async Task<ISshClient> GetClientAsync(string source, CancellationToken cancellationToken)
        {
            source = GUtils.NormalizePath(source);
            var ct = CancellationTokenSource.CreateLinkedTokenSource(this.adapterShutdownToken, cancellationToken);

            if (!this.connections.TryGetValue(source, out var connection))
            {
                if (tryGetSettingsConnection(source, out connection))
                {
                    Assumes.NotNull(connection);
                    this.connections[source] = connection;
                }
                else
                    return await getDefaultAsync(ct.Token).NoAwait();
            }
            return await connection.GetClientAsync(ct.Token).NoAwait();
        }

        private bool tryGetSettingsConnection(string source, [NotNullWhen(true)]out ISshClientConnection? connection)
        {
            connection = null;
            if (this.connectionFactory is null)
            {
                return false;
            }
            var cid = this.settings.Connections?.Find(x => PathUtils.IsSamePath(source, x.TargetPath));
            if (cid is null || cid.Id == 0)
            {
                return false;
            }
            connection = new SshClientConnection(cid.Id, this.connectionFactory);
            return true;
        }

        private async Task<ISshClient> getDefaultAsync(CancellationToken cancellationToken)
        {
            this.defaultConnection ??= await this
                    .defaultConnectionFactory(cancellationToken)
                    .ConfigureAwait(false);
            return this.defaultConnection;
        }
    }
}
