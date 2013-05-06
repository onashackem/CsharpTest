using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client
{
    /// <summary>
    /// Event arguments passed to event when chat message received
    /// </summary>
    class ChatMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Chat message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Nick of message sender
        /// </summary>
        public string User { get; set; }
    }
}
