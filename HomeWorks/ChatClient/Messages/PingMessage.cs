using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    class PingMessage: MessageBase
    {
        private static MessageBase empty;
        public static new MessageBase Empty
        {
            get { return empty; }
        }

        static PingMessage()
        {
            MessageRegEx = new Regex("^PING\n$");
            empty = new PingMessage();
        }

        public PingMessage(): base()
        {
            MessageText = "PING";
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new PingMessage();
        }
    }
}
