using System.ComponentModel;

namespace GoogleTestAdapter.Remote.VisualStudio.Package.OptionsPage
{
    public sealed class CrossPlatformOptionPage : NotifyOptionPage
    {
        internal const string OptionsCategoryName = "Test Adapter for Google Test";
        private VsProjectDeploymentStrategy deploymentStrategy = VsProjectDeploymentStrategy.Outputs;

        [LocalizedDisplayName(nameof(Strings.DeploymentStrategy))]
        [LocalizedDescription(nameof(Strings.DeploymentStrategyDescription))]
        [LocalizedCategory(nameof(Strings.DeploymentCategoryName))]
        [TypeConverter(typeof(EnumTypeConverter))]
        //[PropertyPageTypeConverter(typeof(EnumTypeConverter))]
        public VsProjectDeploymentStrategy DeploymentStrategy
        {
            get => this.deploymentStrategy;
            set => this.SetProperty(ref this.deploymentStrategy, value);
        }
    }
}