using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Client.Messages;

namespace Chat.Client.Core
{
    class MessageParser
    {
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
