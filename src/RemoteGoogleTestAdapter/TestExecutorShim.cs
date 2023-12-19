using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Remote.Adapter.Debugger;
using GoogleTestAdapter.Remote.Adapter.VisualStudio;
using GoogleTestAdapter.Remote.Execution;
using GoogleTestAdapter.Remote.Remoting;
using GoogleTestAdapter.Remote.Runners;
using GoogleTestAdapter.Remote.Settings;
using GoogleTestAdapter.Runners;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Remote.Adapter
{
    partial class TestExecutor
    {
        internal interface ITestExecutorShim
        {
            IInternalTestDiscoverer CreateDiscoverer();
            ISourceDeployment GetDeployment(AdapterSettings settings,
                                            ILogger logger,
                                            ILoggerFactory loggerFactory,
                                            out ISshClientRegistry registry,
                                            CancellationToken token);
            VsIde? GetVsIde();
            void Initialize(IRunContext? runContext,
                            out AdapterSettings settings,
                            IFrameworkHandle? frameworkHandle,
                            out ILogger logger,
                            out ILoggerFactory loggerFactory);
            ITestRunner SelectRunner(AdapterSettings settings,
                                     ITestFrameworkReporter reporter,
                                     IProcessExecutorProvider executorProvider,
                                     ISourceDeployment deployment,
                                     ILogger logger,
                                     CancellationToken cancellationToken);
        }
        internal class TestExecutorShim : ITestExecutorShim
        {
            public IInternalTestDiscoverer CreateDiscoverer() => new TestDiscoverer();

            public ISourceDeployment GetDeployment(AdapterSettings settings,
                                                   ILogger logger,
                                                   ILoggerFactory loggerFactory,
                                                   out ISshClientRegistry registry,
                                                   CancellationToken token)
                => VsVersionUtils.GetDeployment(settings,
                                                logger,
                                                loggerFactory,
                                                out registry,
                                                token);

            public VsIde? GetVsIde() => VsIde.GetInstance();

            public void Initialize(IRunContext? runContext,
                                   out AdapterSettings settings,
                                   IFrameworkHandle? frameworkHandle,
                                   out ILogger logger,
                                   out ILoggerFactory loggerFactory)
            {

                AdapterUtils.Initialize(runContext,
                                        out settings,
                                        frameworkHandle,
                                        out logger,
                                        out loggerFactory);
                settings.DiscoveryMode = AdapterMode.Execution;
            }

            public ITestRunner SelectRunner(AdapterSettings settings,
                                            ITestFrameworkReporter reporter,
                                            IProcessExecutorProvider executorProvider,
                                            ISourceDeployment deployment,
                                            ILogger logger,
                                            CancellationToken cancellationToken)
            {
                VsIde? ide;
                var filters = getOutputTransforms(settings);
                if (settings.IsBeingDebugged
                    && (ide = this.GetVsIde()) is not null)
                {
                    //if (ide is null)
                    //{
                    //    logger.LogError("Failed to retrieve the IDE instance");
                    //}
                    //else
                    //{
                    return new VsDebuggerRunner(ide,
                                                settings,
                                                reporter,
                                                deployment,
                                                logger,
                                                cancellationToken);
                    //}
                }
                return new SequentialRemoteTestRunner(reporter,
                                                      settings,
                                                      deployment,
                                                      executorProvider,
                                                      filters,
                                                      logger);
            }

            private List<ITestOutputFilter> getOutputTransforms(AdapterSettings settings)
            {
                var filters = new List<ITestOutputFilter>();
                if (settings.SourceMap?.Count > 0)
                {
                    filters.Add(new PathMapperFilter(settings.SourceMap));
                }
                return filters;
            }
        }
    }
}
