using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Configuration;

namespace Chat.Client.Messages
{
    class HelloMessage : MessageBase
    {
        public HelloMessage(params string[] versions)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var version in versions)
            {
                if (Versions.IsVersionValid(version))
                    sb.AppendFormat("{0} ", version);
            }

            MessageText = String.Format("HELLO Nprg038Chat {0}", sb.ToString().TrimEnd(new char[] { ' ' }));
        }

        protected override string MessageText { get; set; }
    }
}
