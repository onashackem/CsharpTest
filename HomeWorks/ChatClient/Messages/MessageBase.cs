using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    abstract class MessageBase : IMessage
    {
        protected string MessageText { get; set; }

        protected static Regex MessageRegEx { get; set; }

        public static MessageBase Empty 
        { 
            get { return null; }
        }

        /// <summary>
        /// Gets message in proper format - with a newline at the end
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            bool endsWithNewLine = MessageText.EndsWith("\n");
            
            return MessageText + (endsWithNewLine ? String.Empty : "\n");
        }

        public IMessage Matches(string data)
        {
            var match = MessageRegEx.Match(data);
            if (match.Success)
            {
                return CreateMessage(match);
            }

            return null;
        }

        protected abstract IMessage CreateMessage(Match match);
    }
}
