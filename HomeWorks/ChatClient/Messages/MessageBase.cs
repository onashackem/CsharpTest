using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Client.Messages
{
    abstract class MessageBase : IMessage
    {
        /// <summary>
        /// String that is senf by the message
        /// </summary>
        protected string MessageText { get; set; }

        /// <summary>
        /// Rege that serves for message parsing
        /// </summary>
        protected Regex MessageRegEx { get; set; }

        /// <summary>
        /// Empty template, return null.
        /// </summary>
        public static MessageBase Empty 
        { 
            get { return null; }
        }

        /// <summary>
        /// Contructor with regex for parsing
        /// </summary>
        /// <param name="regex"></param>
        public MessageBase(Regex regex)
        {
            MessageRegEx = regex;
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

        /// <summary>
        /// Matches message (vie regex) from data
        /// </summary>
        /// <param name="data">Data with message</param>
        /// <returns>Returns valid message or null</returns>
        public IMessage Matches(string data)
        {
            var match = MessageRegEx.Match(data);
            if (match.Success)
            {
                return CreateMessage(match);
            }

            return null;
        }

        /// <summary>
        /// Visitor pattern method for proccessing messages
        /// </summary>
        /// <param name="protocol">Protocol that proccesses this message</param>
        public abstract void GetProcessed(Core.ICommunicationProtocol protocol);

        /// <summary>
        /// Creates message from successfully parsed data
        /// </summary>
        /// <param name="match">Regex match</param>
        /// <returns>Valid message</returns>
        protected abstract IMessage CreateMessage(Match match);
    }
}
