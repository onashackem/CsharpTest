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

        /// <summary>
        /// Template message for matching
        /// </summary>
        public static new MessageBase Empty
        {
            get { return empty; }
        }

        /// <summary>
        /// Message text
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Message senders nick
        /// </summary>
        public string From { get; private set; }

        /// <summary>
        /// Creates template message
        /// </summary>
        static ChatMessage()
        {
            empty = new ChatMessage("", "");
        }

        /// <summary>
        /// Contructor with nick and message that defines regex for matching
        /// </summary>
        public ChatMessage(string userName, string message) 
            : base(new Regex("^MSG ([^ ]+) ([^\n]+)\n$"))
        {
            From = userName;
            Message = message;

            MessageText = String.Format("MSG {0} {1}", userName, message);
        }

        /// <summary>
        /// Visotor pattern method - gets processed by communication protocol
        /// </summary>
        /// <param name="protocol">Protocol that processes this message</param>
        public override void GetProcessed(Core.ICommunicationProtocol protocol)
        {
            protocol.ProcessMessage(this);
        }

        /// <summary>
        /// Creates message from successfully matched mattern.
        /// </summary>
        /// <param name="match">Regex pattern match</param>
        /// <returns>Parsed message</returns>
        protected override IMessage CreateMessage(Match match)
        {
            return new ChatMessage(match.Groups[1].ToString(), match.Groups[2].ToString());
        }
    }
}
