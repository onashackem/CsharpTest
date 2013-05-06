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
        /// <summary>
        /// Determines if message matches input data
        /// </summary>
        /// <param name="data">Data with message</param>
        /// <returns>Valid message or null</returns>
        IMessage Matches(string data);

        /// <summary>
        /// Visitor pattern method for proccessing messages
        /// </summary>
        /// <param name="protocol">Protocol that proccesses this message</param>
        void GetProcessed(ICommunicationProtocol protocol);
    }
}
