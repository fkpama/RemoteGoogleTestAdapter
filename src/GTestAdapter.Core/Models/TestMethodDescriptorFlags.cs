namespace GoogleTestAdapter.Remote.Models
{
    [Flags]
    public enum TestMethodDescriptorFlags
    {
        /// <summary>
        /// If <see langword="true" /> we were given
        /// a work folder
        /// </summary>
        ExternalDeployment = 1 << 0,
        /// <summary>
        /// If <see langword="true" /> the directory is to
        /// be deleted when done with it
        /// </summary>
        DeleteDirectory    = 1 << 1
    }
}
