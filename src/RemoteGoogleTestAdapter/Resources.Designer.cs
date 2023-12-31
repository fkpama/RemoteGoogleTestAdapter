﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GoogleTestAdapter.Remote.Adapter {
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
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GoogleTestAdapter.Remote.Adapter.Resources", typeof(Resources).Assembly);
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
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ERROR: {0}.
        /// </summary>
        internal static string ErrorMessage {
            get {
                return ResourceManager.GetString("ErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;xsd:schema xmlns:xsd=&quot;http://www.w3.org/2001/XMLSchema&quot;&gt;
        ///
        ///  &lt;xsd:element name=&quot;GoogleTestAdapterSettings&quot; type=&quot;GoogleTestAdapterSettingsType&quot; /&gt;
        ///
        ///  &lt;xsd:complexType name=&quot;GoogleTestAdapterSettingsType&quot;&gt;
        ///    &lt;xsd:all&gt;
        ///      &lt;xsd:element name=&quot;SolutionSettings&quot; minOccurs=&quot;0&quot; type=&quot;SolutionSettingsType&quot; /&gt;
        ///      &lt;xsd:element name=&quot;ProjectSettings&quot;  minOccurs=&quot;0&quot; type=&quot;ProjectSettingsType&quot;  /&gt;
        ///    &lt;/xsd:all&gt;
        ///  &lt;/xsd:complexType&gt;
        ///
        ///  &lt;xsd:complexType name=&quot;SolutionSettingsType&quot;&gt;
        ///    &lt;xsd:all&gt;
        ///      [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string GoogleTestAdapterSettings {
            get {
                return ResourceManager.GetString("GoogleTestAdapterSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid {0}.
        /// </summary>
        internal static string Invalid {
            get {
                return ResourceManager.GetString("Invalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error loading RunSettings: {0}.
        /// </summary>
        internal static string RunSettingsLoadError {
            get {
                return ResourceManager.GetString("RunSettingsLoadError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Test discovery completed, overall duration: {0}.
        /// </summary>
        internal static string TestDiscoveryCompleted {
            get {
                return ResourceManager.GetString("TestDiscoveryCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exception while discovering tests: {0}.
        /// </summary>
        internal static string TestDiscoveryExceptionError {
            get {
                return ResourceManager.GetString("TestDiscoveryExceptionError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Test Adapter for Remote Google Test: Test Discovery starting{0}..
        /// </summary>
        internal static string TestDiscoveryStarting {
            get {
                return ResourceManager.GetString("TestDiscoveryStarting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Test execution starting..
        /// </summary>
        internal static string TestExecutionStarting {
            get {
                return ResourceManager.GetString("TestExecutionStarting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Trait has same name as base test property and will thus be ignored for test case filtering: {0}.
        /// </summary>
        internal static string TraitIgnoreMessage {
            get {
                return ResourceManager.GetString("TraitIgnoreMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown enum literal: {0}.
        /// </summary>
        internal static string UnknownLiteral {
            get {
                return ResourceManager.GetString("UnknownLiteral", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Warning: {0}.
        /// </summary>
        internal static string WarningMessage {
            get {
                return ResourceManager.GetString("WarningMessage", resourceCulture);
            }
        }
    }
}
