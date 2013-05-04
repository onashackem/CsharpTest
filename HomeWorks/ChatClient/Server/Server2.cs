using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using ChatClient.Core;

namespace Chat.Server
{
    class Server2 : NetworkCommunicator, IDisposable
    {
        private TcpListener server = null;
        
        public void Run() 
        {
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = Configuration.Configuration.ServerPort;
                IPAddress localAddr = IPAddress.Parse(Configuration.Configuration.ServerIpV4Address);

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                new Thread(new ThreadStart(() => Listen())).Start();
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
        }

        private void Listen()
        {
            // Start server
            server.Start();

            // Enter the listening loop.
            while (true)
            {
                //server.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), null);

                var client = server.AcceptTcpClient(); 
                Console.WriteLine("Connected!");

                // Get a stream object for reading and writing
                NetworkStream stream = client.GetStream();

                StartReading(stream);
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            TcpClient client = server.EndAcceptTcpClient(result);
            Console.WriteLine("Connected!");

            // Get a stream object for reading and writing
            NetworkStream stream = client.GetStream();

            StartReading(stream);
        }

        protected override void OnReadFinished(string data, NetworkStream stream)
        {
            Console.WriteLine("Server received: {0}", data);
            StartSending(data, stream);
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
            if (server != null)
                server.Stop();
        }
    }
}
