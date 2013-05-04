using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using ChatClient.Core;
using System.Net;

namespace Chat.Client
{
    class Client2: NetworkCommunicator, IDisposable
    {
        TcpClient client = null;
        NetworkStreamInfo streamInfo = null;
        
        public string Name { get; set; }

        public event EventHandler<MessageEventArgs> MessageRecived;

        public bool TryConnect(string hostAddress)
        {
            if (Connected)
                throw new InvalidOperationException("Client is already connected.");

            try
            {
                var hostEntry = Dns.GetHostEntry(hostAddress);

                if (hostEntry.AddressList.Length == 0)
                    return false;

                IPAddress address = hostEntry.AddressList[0];
                var port = Configuration.Configuration.ServerPort;

                TcpClient client = new TcpClient(address.AddressFamily);
                client.Connect(address, port);
                streamInfo = new NetworkStreamInfo(client.GetStream());

                StartReading(streamInfo);

                Connected = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect: {0}\n{1}", ex.Message, ex.StackTrace);
            }

            return false;
        }

        public void SendMessage(string message)
        {
            StartSending(message, streamInfo);
            Console.WriteLine("Client {1} sent: >>{0}<<", message, Name);
        }

        protected override void OnReadFinished(ReadStateObject readState)
        {
            Console.WriteLine("Client {1} received >>{0}<<", readState.Data, Name);
            
            if (MessageRecived != null)
                MessageRecived(this, new MessageEventArgs() { Data = readState.Data });
        }

        protected override void OnReadingFailed(Exception ex, ReadStateObject readState)
        {
            if (streamInfo != null && streamInfo.Stream != null)
                streamInfo.Stream.Close();            
        }

        protected override void OnSendingFailed(Exception ex, SendStateObject sendState)
        {
            if (streamInfo != null && streamInfo.Stream != null)
                streamInfo.Stream.Close();
        }

        public void Dispose()
        {
            if (streamInfo != null && streamInfo.Stream != null)
                streamInfo.Stream.Close();

            if (client != null)
                client.Close();
        }
    }
}
