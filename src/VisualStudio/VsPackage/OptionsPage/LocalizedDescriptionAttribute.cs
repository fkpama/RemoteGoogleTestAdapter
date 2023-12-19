using System.ComponentModel;

namespace GoogleTestAdapter.Remote.VisualStudio.Package.OptionsPage
{
    sealed class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly string resourceName;

        public override string Description
        {
            get => Strings
                .ResourceManager
                .GetString(resourceName, Strings.Culture);
        }

        public LocalizedDescriptionAttribute(string resourceName)
            : base()
        {
            this.resourceName = resourceName;
        }
    }
}