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

        public string Error { get; private set; }

        static ErrorMessage()
        {
            MessageRegEx = new Regex("^ERROR ([^\n]+)\n$");
            empty = new ErrorMessage("");
        }

        public ErrorMessage(string message)
        {
            Error = message;

            MessageText = String.Format("ERROR {0}", message);
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new ErrorMessage(match.Groups[1].ToString());
        }
    }
}
