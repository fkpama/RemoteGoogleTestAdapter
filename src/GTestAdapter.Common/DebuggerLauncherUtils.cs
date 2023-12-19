using System.Globalization;

namespace GoogleTestAdapter.Remote
{
    public static class DebuggerProxyUtils
    {
        /// <summary>
        /// Relative address of the endpoint interface.
        /// </summary>
        public static readonly Uri InterfaceAddress = new(nameof(IDebuggerLauncher), UriKind.Relative);
        public static Uri ConstructPipeUri(Guid id)
        {
            return ConstructPipeUri(id.ToString());
        }
        public static Uri ConstructPipeUri(string id)
        {
            return new(string.Format(CultureInfo.InvariantCulture, "net.pipe://localhost/RGTA_{0}/", id));
        }
    }
}
