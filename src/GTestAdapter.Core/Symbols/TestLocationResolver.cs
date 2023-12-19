using GoogleTestAdapter.Remote.Remoting;
using GTestAdapter.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Sodiware.Unix.DebugLibrary;

namespace GoogleTestAdapter.Remote.Symbols
{
    public class TestLocationResolver : ITestLocationResolver
    {
        static readonly int TestBodySuffixLen =
            "_Test".Length
            + GoogleTestConstants.TestBodySignature.Length
            + 5;
        readonly ISourceDeployment deployment;
        private readonly ILogger<TestLocationResolver> log;
        private readonly MethodSignatureCreator signatureCreator;

        public TestLocationResolver(ISourceDeployment deployment,
                                    ILogger<TestLocationResolver>? log = null)
        {
            this.deployment = deployment;
            this.log = log.Safe();
            this.signatureCreator = new MethodSignatureCreator();
        }
        public async Task<TestCaseLocation?> ResolveAsync(TestMethodDescriptor descriptor,
                                                              CancellationToken cancellation = default)
        {

            //var method = descriptor.DebugInfo;

            var methods = getDebugInfo(descriptor).ToArray();
            if (methods.Length == 0)
            {
                log.LogWarning("Could not find debug infos for test {suite}.{test}", descriptor.Suite, descriptor.MethodName);
                return null;
            }
            var method = methods[0];
            var line = method.Line;
            //var column = method.Column;

            var fullpath = method.CompilationUnit.FullPath;

            var sourceFileTask = this.deployment
                .MapRemoteFileAsync(descriptor.SourceFile,
                fullpath, cancellation);

            //if (sourceFile.IsMissing())
            //{
            //    log.LogWarning("Could not find source location for test {suite}.{test}", descriptor.Suite, descriptor.MethodName);
            //    //return null;
            //}

            var name = new StringSegment(method.Signature);
            string[]? namespaces = null;
            if (name.HasValue)
            {
                var toRemove = descriptor.Suite.Length
                    + descriptor.MethodName.Length
                    + TestBodySuffixLen;
                if (name.Length > toRemove)
                {
                    var sub = name.Subsegment(0, name.Length - toRemove);
                    if (sub.HasValue)
                    {
                        namespaces = sub.Value.Split(new[] { "::" }, StringSplitOptions.None);
                    }
                }
            }

            var sourceFile = await sourceFileTask.ConfigureAwait(false);
            return new(sourceFile, line, namespaces);
        }

        private IEnumerable<ClassMethodDebugInfo> getDebugInfo(TestMethodDescriptor descriptor)
        {
            var testCase = descriptor.TestCase;
            var binary = descriptor.File;
            foreach (var signature in signatureCreator.GetTestMethodSignatures(testCase))
            {
                var sig = $"{signature}()";
                if (!binary.TryGetMethod(x =>
                {
                    var fname = x.Signature;
                    return fname?.EndsWith(sig, StringComparison.Ordinal) == true;
                }, out var method))
                {
                    continue;
                }

                Assumes.NotNull(method);
                Debug.Assert(method.Signature.IsPresent());
                //var ns = method.Signature.Substring(0, method.Signature.Length - sig.Length);
                yield return method;
            }
        }
    }
}
