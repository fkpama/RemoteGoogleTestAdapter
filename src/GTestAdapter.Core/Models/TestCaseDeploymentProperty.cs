using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Remote.Models
{
    public sealed class TestCaseDeploymentProperty : TestProperty
    {
        public readonly static string Id = typeof(TestCaseDeploymentProperty).FullName!;
        public readonly static string Label = "test case deployment metadata";
        public int ConnectionId { get; }
        public string? RemoteExePath { get; }
        public TestCaseDeploymentProperty(TestListResult result)
            : this(result.ConnectionId, result.RemotePath) { }
        public TestCaseDeploymentProperty(int connectionId, string? remotePath)
            : base(serialize(connectionId, remotePath))
        {
            this.RemoteExePath = remotePath;
            this.ConnectionId = connectionId;
        }

        public static TestCaseDeploymentProperty Parse(string s)
        {
            var items = s.Split(new char[]{ '|' }, StringSplitOptions.RemoveEmptyEntries);
            int connectionId = 0;
            string? remoteExePath = null;
            if (items.Length > 0)
            {
                connectionId = int.Parse(items[0]);
            }
            if (items.Length > 1)
            {
                remoteExePath = items[1];
            }
            return new(connectionId, remoteExePath);

        }

        private static string serialize(int connectionId, string? remotePath)
            => $"|{connectionId}|{remotePath}|";
    }
}
