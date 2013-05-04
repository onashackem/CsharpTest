using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client.Messages
{
    abstract class MessageBase : IMessage
    {
        protected abstract string MessageText { get; set; }

        /// <summary>
        /// Gets message in proper format - with a newline at the end
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            bool endsWithNewLine = MessageText.EndsWith("\n");
            
            return MessageText + (endsWithNewLine ? String.Empty : "\n");
        }
    }
}
