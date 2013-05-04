using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client.Messages
{
    class ErrorMessage: MessageBase
    {
        public ErrorMessage(string message)
        {
            MessageText = String.Format("ERROR {0}", message);
        }

        protected override string MessageText { get; set; }
    }
}
