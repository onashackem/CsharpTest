using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client
{
    class MessageEventArgs : EventArgs
    {
        public string Data { get; set; }
    }
}
