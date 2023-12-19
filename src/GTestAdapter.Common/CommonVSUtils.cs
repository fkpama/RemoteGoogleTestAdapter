using System.Reflection;
namespace GoogleTestAdapter.Remote
{
    public static class CommonVSUtils
    {
        static CommonVSUtils()
        {
        }
        public static void RegisterAssemblyLoad()
        {
            var privateAssemblies = Environment.GetEnvironmentVariable("DevEnvDir")
                ?? Environment.GetEnvironmentVariable("VSAPPIDDIR");
            if (privateAssemblies.IsPresent() && Directory.Exists(privateAssemblies))
            {
                privateAssemblies = Path.Combine(privateAssemblies, "PrivateAssemblies");
                AppDomain.CurrentDomain.AssemblyResolve += (o, e) =>
                {
                    if (e.Name.StartsWith("Microsoft.VisualStudio"))
                    {
                        var asmName = new AssemblyName(e.Name);
                        var fname = $"{asmName.Name}.dll";
                        var path = Path.Combine(privateAssemblies, fname);
                        if (File.Exists(path))
                            return Assembly.LoadFrom(path);
                    }
                    return null;
                };
            }
        }
    }
}
