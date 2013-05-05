using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    class ChatMessage: MessageBase
    {
        private static MessageBase empty;
        public static new MessageBase Empty
        {
            get { return empty; }
        }

        public string Message { get; private set; }

        public string From { get; private set; }

        static ChatMessage()
        {
            MessageRegEx = new Regex("^MSG ([^ ]+) ([^\n]+)\n$");
            empty = new ChatMessage("", "");
        }

        public ChatMessage(string userName, string message) : base()
        {
            From = userName;
            Message = message;

            MessageText = String.Format("MSG {0} {1}", userName, message);
        }
        
        protected override IMessage CreateMessage(Match match)
        {
            return new ChatMessage(match.Groups[1].ToString(), match.Groups[2].ToString());
        }
    }
}
