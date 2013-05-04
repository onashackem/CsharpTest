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

        Dictionary<int, NetworkStreamInfo> streams = new Dictionary<int, NetworkStreamInfo>();
        
        public void Run() 
        {
            try
            {
                Int32 port = Configuration.Configuration.ServerPort;
                IPAddress localAddr = IPAddress.Parse(Configuration.Configuration.ServerIpV4Address);

                // Create server
                server = new TcpListener(localAddr, port);

                new Thread(new ThreadStart(() => Listen())).Start();

                Connected = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to start server: {0}", e.Message);
            }
        }

        private void Listen()
        {
            // Start server
            server.Start();
            int lastId = 0;

            // Enter the listening loop.
            while (true)
            {
                //server.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), null);

                var client = server.AcceptTcpClient(); 
                Console.WriteLine("Connected!");

                // Get a stream object for reading and writing
                var streamInfo = new NetworkStreamInfo(client.GetStream(), lastId);
                streams.Add(lastId, streamInfo);

                StartReading(streamInfo);

                ++lastId;
            }
        }

        protected override void OnReadFinished(ReadStateObject readState)
        {
            Console.WriteLine("Server received: >{0}<", readState.Data);

            // Sent to all clients
            var data = readState.Data;
            foreach (var info in streams.Values)
            {
                StartSending(readState.Data, info);
            }
        }

        protected override void OnReadingFailed(Exception ex, ReadStateObject readState)
        {
            RemoveStreamInfo(readState.StreamInfo.ID);
        }

        protected override void OnSendingFailed(Exception ex, SendStateObject sendState)
        {
            RemoveStreamInfo(sendState.StreamInfo.ID);
        }

        private void RemoveStreamInfo(int streamInfoId)
        {
            streams.Remove(streamInfoId);
        }

        public void Dispose()
        {
            if (server != null)
                server.Stop();
        }
    }
}
