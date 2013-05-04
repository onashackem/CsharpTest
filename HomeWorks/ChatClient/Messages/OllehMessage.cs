using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Configuration;

namespace Chat.Client.Messages
{
    class OllehMessage: MessageBase
    {
        public OllehMessage(string version)
        {
            StringBuilder sb = new StringBuilder();

            System.Diagnostics.Debug.Assert(Versions.IsVersionValid(version), "Invalid version: " + version);
            
            MessageText = String.Format("OLLEH Nprg038Chat {0}", version);
        }

        protected override string MessageText { get; set; }
    }
}
