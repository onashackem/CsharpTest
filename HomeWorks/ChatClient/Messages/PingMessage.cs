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

        protected static new Regex MessageRegEx { get; set; }

        static PingMessage()
        {
            empty = new PingMessage();
        }

        public PingMessage()
            : base(new Regex("^PING\n$"))
        {
            MessageText = "PING";
        }

        public override void GetProcessed(Core.ICommunicationProtocol protocol)
        {
            protocol.ProcessMessage(this);
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new PingMessage();
        }
    }
}
