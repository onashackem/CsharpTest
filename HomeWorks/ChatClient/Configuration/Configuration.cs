using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Configuration
{
    /// <summary>
    /// Internal char configuration
    /// </summary>
    class Configuration
    {
        /// <summary>
        /// Default ip address (where server listens on)
        /// </summary>
        public static string ServerIpV4Address { get { return "127.0.0.1"; } }

        /// <summary>
        /// Port where server listens on
        /// </summary>
        public static int ServerPort { get { return 4586; } }
    }
}
