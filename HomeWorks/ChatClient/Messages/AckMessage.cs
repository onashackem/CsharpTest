using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client.Messages
{
    class AckMessage: MessageBase
    {
        public AckMessage()
        {
            MessageText = "ACK";
        }

        protected override string MessageText { get; set; }
    }
}
