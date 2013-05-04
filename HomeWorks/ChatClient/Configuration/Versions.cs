using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Configuration
{
    public static class Versions
    {
        public static readonly string VERSION_1_0 = "1.0";

        public static readonly string VERSION_1_1 = "1.1";

        public static bool IsVersionValid(string version)
        {
            return version.Equals(VERSION_1_0) || version.Equals(VERSION_1_1);
        }
    }
}
