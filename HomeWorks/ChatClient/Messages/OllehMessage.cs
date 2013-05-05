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
        private static MessageBase empty;
        public static new MessageBase Empty
        {
            get { return empty; }
        }

        protected static new Regex MessageRegEx { get; set; }

        public string Version { get; private set; }

        static OllehMessage()
        {
            empty = new OllehMessage("");
        }

        public OllehMessage(string version)
            : base(new Regex("^OLLEH Nprg038Chat ([^\n]+)\n$"))
        {
            StringBuilder sb = new StringBuilder();

            Version = version;
            
            MessageText = String.Format("OLLEH Nprg038Chat {0}", version);
        }

        public override void GetProcessed(Core.ICommunicationProtocol protocol)
        {
            protocol.ProcessMessage(this);
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new OllehMessage(match.Groups[1].ToString());
        }
    }
}
