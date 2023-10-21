using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using RemoteGoogleTestAdapter.Utils;

namespace RemoteGoogleTestAdapter
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum VsVersion
    {
        Unknown = -1, VS2012 = 0, VS2012_1 = 11, VS2013 = 12, VS2015 = 14, VS2017 = 15, VS2019 = 16, VS2022 = 17
    }


    internal static class VsVersionExtesions
    {
    }
}
