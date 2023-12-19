namespace GoogleTestAdapter.Remote.Models
{
    public struct TestListMetadata
    {

        public IReadOnlyDictionary<string, int> NbTestPerSuite { get; }
        public int TotalTestInExe { get; }
        public TestListMetadata(IReadOnlyDictionary<string, int> nbTestsPerSuite,
                                int totalTestsInExe)
        {
            this.NbTestPerSuite = nbTestsPerSuite;
            this.TotalTestInExe = totalTestsInExe;
        }
    }
    public struct TestListResult
    {
        public int ConnectionId { get; set; }
        public string RemotePath { get; set; }
        public string[] Outputs { get; set; }
        public TestListResult()
        {
            this.ConnectionId = -1;
            this.RemotePath = null!;
            this.Outputs = Array.Empty<string>();
        }

        public TestListResult(int connectionId,
                              string remotePath,
                              string[] outputs)
        {
            this.ConnectionId = connectionId;
            this.RemotePath = remotePath;
            this.Outputs = outputs;
        }
    }
}
