using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace GoogleTestAdapter.Remote.VisualStudio.Package.OptionsPage
{
    [PropertyPageTypeConverter(typeof(EnumTypeConverter))]
    public enum VsProjectDeploymentStrategy
    {
        [LocalizedDescription(nameof(Strings.ProjectOutputDeploymentStrategyDescription))]
        [LocalizedName(typeof(Strings), nameof(Strings.ProjectOutputDeploymentStrategyName))]
        OutDir = DeploymentStrategy.OutDir,
        [LocalizedDescription(nameof(Strings.OutputDirectoryDeploymentStrategyDescription))]
        [LocalizedName(typeof(Strings), nameof(Strings.OutputDirectoryDeploymentStrategyName))]
        Outputs = DeploymentStrategy.Outputs
    }
}