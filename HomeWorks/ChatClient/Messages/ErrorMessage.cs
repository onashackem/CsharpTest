using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    class ErrorMessage: MessageBase
    {
        private static MessageBase empty = new ErrorMessage("");
        private static Regex regex = new Regex("^ERROR ([^\n]+)\n$");

        /// <summary>
        /// Template message for matching
        /// </summary>
        public static new IMessage Empty
        {
            get { return empty; }
        }

        protected override Regex MessageRegEx
        {
            get { return regex; }
        }

        /// <summary>
        /// Error that occured
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Contructor with error text that defines regex for matching
        /// </summary>
        public ErrorMessage(string message) 
            : base ()
        {
            Error = message;

            MessageText = String.Format("ERROR {0}", message);
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
            return new ErrorMessage(match.Groups[1].ToString());
        }
    }
}
