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

        public string[] AvailableVersions { get; private set; }

        static HelloMessage()
        {
            MessageRegEx = new Regex("^HELLO Nprg038Chat ([^\n]+)\n$");
            empty = new HelloMessage();
        }

        public HelloMessage(params string[] versions)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var version in versions)
            {
                sb.AppendFormat("{0} ", version);
            }

            var v = sb.ToString();
            var sep = new char[] { ' ' };

            AvailableVersions = v.Split(sep, StringSplitOptions.RemoveEmptyEntries);

            MessageText = String.Format("HELLO Nprg038Chat {0}", v.TrimEnd(sep));
        }

        protected override IMessage CreateMessage(Match match)
        {
            return new HelloMessage(match.Groups[1].ToString());
        }
    }
}
