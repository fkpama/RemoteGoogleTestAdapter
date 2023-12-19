using GTestAdapter.Core.Models;

namespace GoogleTestAdapter.Remote.Symbols
{
    public interface ITestLocationResolver
    {
        Task<TestCaseLocation?> ResolveAsync(TestMethodDescriptor descriptor,
                                               CancellationToken cancellation);
    }
}