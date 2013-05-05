using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Client.Messages;

namespace Chat.Client.Core
{
    interface ICommunicationProtocol
    {
        void ProcessMessage(AckMessage message);
                
        void ProcessMessage(ErrorMessage message);

        void ProcessMessage(HelloMessage message);

        void ProcessMessage(ChatMessage message);

        void ProcessMessage(OllehMessage message);

        void ProcessMessage(PingMessage message);

        void ProcessMessage(PongMessage message);
    }
}
