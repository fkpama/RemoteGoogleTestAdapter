namespace GoogleTestAdapter.Remote.Execution
{
    public interface IProcessExecutorProvider
    {
        Task<IProcessExecutor> GetExecutorAsync(TestCase testCase, CancellationToken cancellationToken);
    }
}
