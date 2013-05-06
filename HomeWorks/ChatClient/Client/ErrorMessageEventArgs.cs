using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client
{
    /// <summary>
    /// Event arguments passed to event when error message received
    /// </summary>
    class ErrorMessageEventArgs : EventArgs
    {
        /// <summary>
        /// The error message
        /// </summary>
        public string Error { get; set; }
    }
}
