using GoogleTestAdapter.DiaResolver;
using GTestAdapter.Core.Models;

namespace GTestAdapter.Core.Binary
{
    public interface ITestLocationResolver
    {
        Task<SourceFileLocation?> ResolveAsync(TestMethodDescriptor descriptor,
                                               CancellationToken cancellation);
    }
}