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

        protected static new Regex MessageRegEx { get; set; }

        static PongMessage()
        {
            empty = new PongMessage();
        }

        public PongMessage()
            : base(new Regex("^PONG\n$"))
        {
            MessageText = "PONG";
        }

        public override void GetProcessed(Core.ICommunicationProtocol protocol)
        {
            protocol.ProcessMessage(this);
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new PongMessage();
        }
    }
}
