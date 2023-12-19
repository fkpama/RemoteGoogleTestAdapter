

namespace GoogleTestAdapter.Remote.VisualStudio.Package.OptionsPage
{
    sealed class LocalizedDisplayNameAttribute : System.ComponentModel.DisplayNameAttribute
    {
        public LocalizedDisplayNameAttribute(string resourceName)
            : base(Strings
                .ResourceManager
                .GetString(resourceName, Strings.Culture))
        {

        }
    }
}