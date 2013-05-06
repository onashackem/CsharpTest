using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    /// <summary>
    /// Message that says "ACK"
    /// </summary>
    class AckMessage: MessageBase
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
        /// Creates template message
        /// </summary>
        static AckMessage()
        {
            empty = new AckMessage();
        }

        /// <summary>
        /// Contructor that defines regex for matching
        /// </summary>
        public AckMessage()
            : base(new Regex("^ACK\n$"))
        {
            MessageText = "ACK";
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
            return new AckMessage();
        }
    }
}
