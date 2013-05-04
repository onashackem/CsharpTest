using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client.Messages
{
    class ChatMessage: MessageBase
    {
        public ChatMessage(string userName, string message)
        {
            MessageText = String.Format("MSG {0} {1}", userName, message);
        }

        protected override string MessageText { get; set; }
    }
}
