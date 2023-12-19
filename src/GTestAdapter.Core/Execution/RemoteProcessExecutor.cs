using System.Text;
using GoogleTestAdapter.Remote.Remoting;

namespace GoogleTestAdapter.Remote.Execution
{
    internal sealed class RemoteProcessExecutor : IProcessExecutor
    {
        private readonly ISourceDeployment deployment;
        private readonly ISshClient client;
        private readonly ILogger logger;
        private readonly CancellationTokenSource cts;

        public RemoteProcessExecutor(ISourceDeployment deployment,
                                     ISshClient client,
                                     ILogger logger,
                                     CancellationToken cancellationToken)
        {
            this.deployment = deployment;
            this.client = client;
            this.logger = logger;
            this.cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public void Cancel()
        {
            this.cts.Cancel();
        }

        public int ExecuteCommandBlocking(string command,
                                          string parameters,
                                          string workingDir,
                                          IDictionary<string, string> envVars,
                                          string pathExtension,
                                          Action<string> reportOutputLine)
        {
            Assumes.Null(pathExtension);
            IReadOnlyDictionary<string, string>? env = envVars as IReadOnlyDictionary<string, string>
                ?? (envVars is null ? null : new Dictionary<string, string>(envVars));
            var outputWriter = new ReportLineTextWriter(reportOutputLine);
            var rc = Task.Run(async () =>
            {
                return await this.client.RunCommandAsync(command,
                                                           parameters,
                                                           workingDir: workingDir,
                                                           envVars: env,
                                                           null,
                                                           outputWriter: outputWriter,
                                                           errorWriter: outputWriter,
                                                           this.cts.Token)
                .ConfigureAwait(false);
            }, cts.Token).GetAwaiter().GetResult();
            return rc;
        }
    }

    internal sealed class ReportLineTextWriter : TextWriter
    {
        private readonly Action<string> lineAction;
        private readonly StringBuilder sb = new();
        public ReportLineTextWriter(Action<string> lineAction)
        {
            this.lineAction = lineAction;
        }

        public override Encoding Encoding { get; } = Encoding.UTF8;
        public string CurrentLine => sb.ToString();

        public override void Write(string? value)
        {
            if (value is null) return;
            lock (this.sb)
            {
                var lines = value.Split('\n');
                int i = 0;
                if (lines.Length > 1)
                {
                    sb.Append(lines[i++]);
                    lineAction(sb.ToString());
                    sb.Clear();
                }
                foreach (var line in lines.Skip(i).Take(lines.Length - 1 - i))
                {
                    lineAction(line);
                }

                var last = lines[lines.Length - 1];
                if (!value.EndsWith("\n", StringComparison.Ordinal))
                    sb.Append(last);

            }
        }
        public override void Write(char value)
        {
            lock (this.sb)
            {
                if (value == '\n')
                {
                    lineAction(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(value);
                }
            }
        }
    }
}
