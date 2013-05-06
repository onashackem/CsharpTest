using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Configuration
{
    /// <summary>
    /// Available protocol versions of client-server communication
    /// </summary>
    public class Versions
    {
        /// <summary>
        /// Simple version
        /// </summary>
        public static readonly string VERSION_1_0 = "1.0";

        /// <summary>
        /// Version with ping-pong messages
        /// </summary>
        public static readonly string VERSION_1_1 = "1.1";

        /// <summary>
        /// Validates version name
        /// </summary>
        /// <param name="version">Version to validate</param>
        /// <returns>True if version is valid</returns>
        public static bool IsVersionValid(string version)
        {
            return version.Equals(VERSION_1_0) || version.Equals(VERSION_1_1);
        }
    }
}
