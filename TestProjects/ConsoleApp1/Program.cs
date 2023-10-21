using GoogleTestAdapter.Framework;
using GTestAdapter.Core.Settings;
using RemoteGoogleTestAdapter;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteGoogleTestAdapter.IDE;
using Microsoft.VisualStudio.LocalLogger;
using Microsoft;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
const string output = @"
Running main() from /build/googletest-YnT0O3/googletest-1.10.0.20201025/googletest/src/gtest_main.cc
SampleTest.
  sample_test1
ExampleTest.
  example_test1
";

var dir = Environment.CurrentDirectory;
var path = "SimpleTestProject.out";
//if(!ElfBinaryDiscoverer.IsGTestBinary(path))
//{
//    return -1;
//}

//var client = ConnectionFactory.CreateClient();

//var remotePath = "/home/fred/test.out";
//remotePath = liblinux.IO.PathUtils.NormalizePathSlow(remotePath);
//remotePath = liblinux.IO.PathUtils.EscapeFilenameForUnixShell(remotePath);
//await client.UploadAsync(path, remotePath, CancellationToken.None);

//await client.RunCommandAsync($"chmod +x {remotePath}");


//var output = await client.ExecCommandAsync($"{remotePath} --gtest_list_tests");


//if (string.IsNullOrWhiteSpace(output))
//{
//    return - 1;
//}


//var logger = LoggerFactory
//    .Create(b => b
//    .AddConsole()
//    .AddFile("ConsoleApp.log", append: true)
//    .SetMinimumLevel(LogLevel.Debug));
//var discoverer = new TestDiscoverer();
//var reporter = new Mock<ITestFrameworkReporter>();
//var source = Path.GetFullPath("SimpleTestProject.out");
//var settings = new AdapterSettings();

//await discoverer.DiscoverTestsAsync(new[] { source },
//    settings,
//    logger,
//    reporter.Object);

var logger = new Mock<GoogleTestAdapter.Common.ILogger>();

if(VsIde.TryGetInstance(32808, logger.Object, out var ide))
{
    Assumes.NotNull(ide);
    var project = ide.GetProjectForOutputPath("SimpleTestProject.out");

    if (project is not null)
    {
        //var id = await project.GetSshClientAsync();
        //var o = await project.GetListTestOutputAsync();
        project.GetFileMapping();
    }
}

return 0;
