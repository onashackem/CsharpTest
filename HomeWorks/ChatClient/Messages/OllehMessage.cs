using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Configuration;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    class OllehMessage: MessageBase
    {
        private static MessageBase empty = new OllehMessage("");
        private static Regex regex = new Regex("^OLLEH Nprg038Chat ([^\n]+)\n$");

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
        /// Aggreed version by server
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Contructor with agreed version that defines regex for matching
        /// </summary>
        public OllehMessage(string version)
            : base()
        {
            StringBuilder sb = new StringBuilder();

            Version = version;
            
            MessageText = String.Format("OLLEH Nprg038Chat {0}", version);
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
            return new OllehMessage(match.Groups[1].ToString());
        }
    }
}
