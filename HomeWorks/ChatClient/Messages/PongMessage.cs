using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client.Messages
{
    class PongMessage: MessageBase
    {
        public PongMessage(string message)
        {
            MessageText = "PONG";
        }

        protected override string MessageText { get; set; }
    }
}
