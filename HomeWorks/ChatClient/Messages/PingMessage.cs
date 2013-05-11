using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    class PingMessage: MessageBase
    {
        private static MessageBase empty = new PingMessage();
        private static Regex regex = new Regex("^PING\n$");

        /// <summary>
        /// Template message for matching
        /// </summary>
        public static new MessageBase Empty
        {
            get { return empty; }
        }

        protected override Regex MessageRegEx
        {
            get { return regex; }
        }

        /// <summary>
        /// Contructor that defines regex for matching
        /// </summary>
        public PingMessage()
            : base()
        {
            MessageText = "PING";
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
            return new PingMessage();
        }
    }
}
