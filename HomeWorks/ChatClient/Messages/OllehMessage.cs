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

        public string Version { get; private set; }

        static OllehMessage()
        {
            MessageRegEx = new Regex("^OLLEH Nprg038Chat ([^\n]+)\n$");
            empty = new OllehMessage("");
        }

        public OllehMessage(string version) : base()
        {
            StringBuilder sb = new StringBuilder();

            Version = version;
            
            MessageText = String.Format("OLLEH Nprg038Chat {0}", version);
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new OllehMessage(match.Groups[1].ToString());
        }
    }
}
