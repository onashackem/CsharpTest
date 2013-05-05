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
        private static MessageBase empty;
        public static new MessageBase Empty
        {
            get { return empty; }
        }

        protected static new Regex MessageRegEx { get; set; }

        public string[] AvailableVersions { get; private set; }

        static HelloMessage()
        {
            empty = new HelloMessage("");
        }

        public HelloMessage(string versions)
            : base(new Regex("^HELLO Nprg038Chat ([^\n]+)\n$"))
        {
            var sep = new char[] { ' ' };

            AvailableVersions = versions.Split(sep, StringSplitOptions.RemoveEmptyEntries);

            MessageText = String.Format("HELLO Nprg038Chat {0}", versions.TrimEnd(sep));
        }

        public override void GetProcessed(Core.ICommunicationProtocol protocol)
        {
            protocol.ProcessMessage(this);
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new HelloMessage(match.Groups[1].ToString());
        }
    }
}
