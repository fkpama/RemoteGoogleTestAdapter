﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GoogleTestAdapter.Remote.VisualStudio.Package {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GoogleTestAdapter.Remote.VisualStudio.Package.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deployment.
        /// </summary>
        public static string DeploymentCategoryName {
            get {
                return ResourceManager.GetString("DeploymentCategoryName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deployment Method.
        /// </summary>
        public static string DeploymentStrategy {
            get {
                return ResourceManager.GetString("DeploymentStrategy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Controls how to deploy projects executable to the remote system.
        /// </summary>
        public static string DeploymentStrategyDescription {
            get {
                return ResourceManager.GetString("DeploymentStrategyDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deploy the entire output directory and its content.
        /// </summary>
        public static string OutputDirectoryDeploymentStrategyDescription {
            get {
                return ResourceManager.GetString("OutputDirectoryDeploymentStrategyDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Output directory.
        /// </summary>
        public static string OutputDirectoryDeploymentStrategyName {
            get {
                return ResourceManager.GetString("OutputDirectoryDeploymentStrategyName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deploy only the project outputs.
        /// </summary>
        public static string ProjectOutputDeploymentStrategyDescription {
            get {
                return ResourceManager.GetString("ProjectOutputDeploymentStrategyDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Project output.
        /// </summary>
        public static string ProjectOutputDeploymentStrategyName {
            get {
                return ResourceManager.GetString("ProjectOutputDeploymentStrategyName", resourceCulture);
            }
        }
    }
}
