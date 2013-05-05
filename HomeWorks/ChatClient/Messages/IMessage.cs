using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Chat.Client.Core;

namespace Chat.Client.Messages
{
    interface IMessage
    {
        IMessage Matches(string data);

        void GetProcessed(ICommunicationProtocol protocol);
    }
}
