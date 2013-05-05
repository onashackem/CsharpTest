using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    class PongMessage: MessageBase
    {
        private static MessageBase empty;
        public static new MessageBase Empty
        {
            get { return empty; }
        }

        static PongMessage()
        {
            MessageRegEx = new Regex("^PONG\n$");
            empty = new PongMessage();
        }

        public PongMessage() : base()
        {
            MessageText = "PONG";
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new PongMessage();
        }
    }
}
