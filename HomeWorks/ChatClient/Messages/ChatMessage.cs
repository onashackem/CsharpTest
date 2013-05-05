﻿using System;
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

        protected static new Regex MessageRegEx { get; set; }

        public string Message { get; private set; }

        public string From { get; private set; }

        static ChatMessage()
        {
            empty = new ChatMessage("", "");
        }

        public ChatMessage(string userName, string message) 
            : base(new Regex("^MSG ([^ ]+) ([^\n]+)\n$"))
        {
            From = userName;
            Message = message;

            MessageText = String.Format("MSG {0} {1}", userName, message);
        }

        public override void GetProcessed(Core.ICommunicationProtocol protocol)
        {
            protocol.ProcessMessage(this);
        }
        
        protected override IMessage CreateMessage(Match match)
        {
            return new ChatMessage(match.Groups[1].ToString(), match.Groups[2].ToString());
        }
    }
}
