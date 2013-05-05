﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    class AckMessage: MessageBase
    {
        private static MessageBase empty;
        public static new MessageBase Empty
        {
            get { return empty; }
        }

        static AckMessage()
        {
            MessageRegEx = new Regex("^ACK\n$");
            empty = new AckMessage();
        }

        public AckMessage() : base()
        {
            MessageText = "ACK";
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new AckMessage();
        }
    }
}