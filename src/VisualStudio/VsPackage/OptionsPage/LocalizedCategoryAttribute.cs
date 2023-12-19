using System.ComponentModel;

namespace GoogleTestAdapter.Remote.VisualStudio.Package.OptionsPage
{
    sealed class LocalizedCategoryAttribute : CategoryAttribute
    {
        private readonly string ResourceName;

        public LocalizedCategoryAttribute(string resourceName)
            : base()
        {
            this.ResourceName = resourceName;
        }

        protected override string GetLocalizedString(string value)
        {
            return Strings.ResourceManager
                .GetString(ResourceName, Strings.Culture);
        }
    }
}