using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using ChatClient.Core;

namespace Chat.Client
{
    class Client2: NetworkCommunicator, IDisposable
    {
        TcpClient client = null;
        NetworkStream stream = null;
        
        public string Name { get; set; }

        public void Run() 
        {
            try
            {
                var server = Configuration.Configuration.ServerIpV4Address;
                var port = Configuration.Configuration.ServerPort;

                TcpClient client = new TcpClient(server, port);
                stream = client.GetStream();

                StartReading(stream);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to init: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        public void SendMessage(string message)
        {
            StartSending(message, stream);
            Console.WriteLine("Sent: {0}", message);
        }

        protected override void OnReadFinished(string data, NetworkStream stream)
        {
            Console.WriteLine("Client {0} received {1}", Name, data);
        }

        protected override void OnReadingFailed(Exception ex, NetworkStream stream, ReadStateObject state)
        {
            throw new NotImplementedException();
        }

        protected override void OnSendingFailed(Exception ex, NetworkStream stream, SendStateObject state)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (stream != null)
                stream.Close();

            if (client != null)
                client.Close();
        }
    }
}
