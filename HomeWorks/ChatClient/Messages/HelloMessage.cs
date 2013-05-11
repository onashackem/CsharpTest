using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Configuration;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    class HelloMessage : MessageBase
    {
        private static MessageBase empty = new HelloMessage("");
        private static Regex regex = new Regex("^HELLO Nprg038Chat ([^\n]+)\n$");

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
        /// Available versions of client
        /// </summary>
        public string[] AvailableVersions { get; private set; }

        /// <summary>
        /// Contructor with offered versions that defines regex for matching
        /// </summary>
        public HelloMessage(string versions)
            : base()
        {
            var sep = new char[] { ' ' };

            AvailableVersions = versions.Split(sep, StringSplitOptions.RemoveEmptyEntries);

            MessageText = String.Format("HELLO Nprg038Chat {0}", versions.TrimEnd(sep));
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
            return new HelloMessage(match.Groups[1].ToString());
        }
    }
}
