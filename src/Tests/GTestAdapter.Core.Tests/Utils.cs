using System.Globalization;

namespace GTestAdapter.Core.Tests
{
    internal class Utils
    {
        public static void TimestampMessage(ref string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            message = $"{timestamp} - {message ?? ""}";
        }
        internal static string RemoveRunningMain(string testListOutput)
        {
            var lst = testListOutput.Trim();
            if (lst.StartsWith("Running main() from"))
            {
                var idx = lst.IndexOf('\n');
                lst = lst.Substring(idx + 1);
            }
            return lst.Trim();
        }

        internal static void SafeDelete(string file)
        {
            if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
            {
                int tryCount;
                for(tryCount = 0; tryCount < 10; tryCount++)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    when (tryCount < 9)
                    {
                        Thread.Sleep(1000);
                    }
                    catch (IOException ex)
                    when (tryCount == 9)
                    {
                        Console.Error.WriteLine($"* WARNING *: Failed to delete file {file}: {ex.Message}");
                    }
                }
            }
        }
    }
}
