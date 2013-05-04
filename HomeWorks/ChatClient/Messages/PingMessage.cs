using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client.Messages
{
    class PingMessage: MessageBase
    {
        public PingMessage(string message)
        {
            MessageText = "PING";
        }

        protected override string MessageText { get; set; }
    }
}
