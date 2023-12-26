namespace GoogleTestAdapter.Remote.Models
{
    public sealed class TestCaseDeploymentProperty : TestProperty
    {
        public readonly static string Id = typeof(TestCaseDeploymentProperty).FullName!;
        public readonly static string Label = "test case deployment metadata";
        public int ConnectionId { get; }
        public string? RemoteExePath { get; }
        public TestMethodDescriptorFlags Flags { get; }
        public TestCaseDeploymentProperty(TestListResult result)
            : this(result.ConnectionId, result.RemotePath, result.Flags) { }
        public TestCaseDeploymentProperty(int connectionId, string? remotePath, TestMethodDescriptorFlags flags)
            : base(serialize(connectionId, remotePath, flags))
        {
            this.RemoteExePath = remotePath;
            this.ConnectionId = connectionId;
        }

        public static TestCaseDeploymentProperty Parse(string s)
        {
            var items = s.Split(new char[]{ '|' }, StringSplitOptions.RemoveEmptyEntries);
            var connectionId = int.Parse(items[0]);
            var remoteExePath = items[1];
            TestMethodDescriptorFlags flags = 0;
            if (items.Length > 1)
            {
                var val = items[2];
                if (int.TryParse(val, out var ival))
                    flags = (TestMethodDescriptorFlags)ival;
            }
            return new(connectionId, remoteExePath, flags);

        }

        private static string serialize(int connectionId, string? remotePath, TestMethodDescriptorFlags flags)
            => $"|{connectionId}|{remotePath}{(flags != 0 ? $"|{(int)flags}" : null)}|";
    }
}
