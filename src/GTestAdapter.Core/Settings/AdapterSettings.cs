namespace GoogleTestAdapter.Remote.Settings
{
    public enum AdapterMode
    {
        Discovery = 0,
        Execution = 1
    }
    public class AdapterSettings
    {
        private List<Func<Task>>? cleanups;
        public IReadOnlyList<Func<Task>> CleanupActions
            => (IReadOnlyList<Func<Task>>?)cleanups ?? Array.Empty<Func<Task>>();

        public virtual bool CollectSourceInformation { get; }
        public virtual string? OverrideSource { get; }
        public virtual Guid? DebuggerPipeId { get; }
        public virtual bool DebugMode { get; }
        public virtual int NrOfTestRepetitions { get; }
        public virtual TimeSpan TestDiscoveryTimeout { get; }
        public virtual bool IsRunningInsideVisualStudio { get; }
        public virtual bool IsBeingDebugged { get; }
        public virtual string RemoteDeploymentDirectory { get; } = "/tmp";
        public virtual bool TimestampOutput { get; } = true;
        public AdapterMode DiscoveryMode { get; set; } = AdapterMode.Discovery;

        public virtual SettingsWrapper GetWrapper()
        {
            return new();
        }

        public virtual List<ConnectionId>? Connections { get; }
        public virtual List<SourceMap>? SourceMap { get; }

        internal void AddCleanup(Func<Task> callback)
        {
            (this.cleanups ??= new()).Add(callback);
        }
        public void ExecuteCleanups(ILogger? logger)
        {
            if (this.cleanups is null)
            {
                return;
            }

            var tasks = new List<Task>(this.cleanups.Count);
            foreach(var cleanup in this.cleanups)
            {
                var callback = cleanup;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await callback().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning($"Failed to execute cleanup task: {ex.Message}");
                    }
                }));
            }

            cleanups = null;
            Task.WhenAll(tasks).GetAwaiter().GetResult();
        }

    }
}
