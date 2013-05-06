using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Client.Messages;

namespace Chat.Client.Core
{
    /// <summary>
    /// Parses received messages from strings
    /// </summary>
    class MessageParser
    {
        /// <summary>
        /// Empty messages samples to match from string
        /// </summary>
        protected List<IMessage> emptyMessages = new List<IMessage>()
        {
            PingMessage.Empty,
            PongMessage.Empty,
            AckMessage.Empty,
            HelloMessage.Empty,
            HelloMessage.Empty,            
            OllehMessage.Empty,
            OllehMessage.Empty,
            ChatMessage.Empty,
            ChatMessage.Empty,
            ChatMessage.Empty,
            ErrorMessage.Empty,
        };

        /// <summary>
        /// Finds message type that matches received data. If no match found, ErrorMessage is returned.
        /// </summary>
        /// <param name="data">Received data</param>
        /// <returns>Finds parsed message, Error message when not match found</returns>
        public virtual IMessage ParseMessage(string data)
        {
            foreach (var emptyMessage in emptyMessages)
            {
                var message = emptyMessage.Matches(data);

                // If matches
                if (message != null)
                    return message;
            }

            // Not matched - unknown message or bad format 
            return new ErrorMessage(data);
        }
    }
}
