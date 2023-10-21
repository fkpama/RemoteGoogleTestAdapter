namespace GTestAdapter.Core.Settings
{
    public class AdapterSettings
    {
        public virtual bool CollectSourceInformation { get; } = true;
        public virtual string? OverrideSource { get; }
    }
}
