using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client
{
    class ChatMessageEventArgs : EventArgs
    {
        public string Message { get; set; }

        public string User { get; set; }
    }
}
