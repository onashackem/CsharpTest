using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    class ErrorMessage: MessageBase
    {
        private static MessageBase empty;
        public static new MessageBase Empty
        {
            get { return empty; }
        }

        protected static new Regex MessageRegEx { get; set; }

        public string Error { get; private set; }

        static ErrorMessage()
        {
            empty = new ErrorMessage("");
        }

        public ErrorMessage(string message) 
            : base (new Regex("^ERROR ([^\n]+)\n$"))
        {
            Error = message;

            MessageText = String.Format("ERROR {0}", message);
        }

        public override void GetProcessed(Core.ICommunicationProtocol protocol)
        {
            protocol.ProcessMessage(this);
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new ErrorMessage(match.Groups[1].ToString());
        }
    }
}
