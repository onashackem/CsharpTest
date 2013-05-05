using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using ChatClient.Core;
using Chat.Client.Messages;
using Chat.Client.Core.Protocol;

namespace Chat.Server
{
    class Server : NetworkCommunicator, IDisposable
    {
        private TcpListener server = null;

        Dictionary<int, ClientInfo> clients;

        public Server()
        {
            clients = new Dictionary<int, ClientInfo>();
        }
        
        public virtual void Run() 
        {
            try
            {
                Int32 port = Configuration.Configuration.ServerPort;
                IPAddress address = GetIPAddress(Configuration.Configuration.ServerIpV4Address);

                if (address == null)
                    throw new InvalidOperationException("Invalid IP address: " + Configuration.Configuration.ServerIpV4Address);

                // Create server
                server = new TcpListener(address, port);

                new Thread(new ThreadStart(() => Listen())).Start();

                Connected = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to start server: {0}", e.Message);
            }
        }

        /// <summary>
        /// Sends message to client with specified id
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="clientId">Id of receiver</param>
        public virtual void SendMessage(IMessage message, int clientId)
        {
            // Client may be removed for inactivity
            if (!clients.ContainsKey(clientId))
                return;

            var client = clients[clientId];
            
            SendMessageToClient(message, client);
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="message">Message to send to every client</param>
        public virtual void SendMessage(IMessage message)
        {
            // Sent to all clients
            foreach (var client in clients.Values)
            {
                SendMessageToClient(message, client);
            }
        }

        protected virtual void Listen()
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
                var clientInfo = new ClientInfo(client.GetStream(), lastId, new ServerProtocol(this, lastId));

                clients.Add(lastId, clientInfo);

                StartReading(clientInfo);

                ++lastId;
            }
        }

        protected override void OnReadFinished(IMessage message, ReadStateObject readState)
        {
            Console.WriteLine("Server received: >{0}<", message);

            readState.ClientInfo.LastContact = DateTime.Now;
            message.GetProcessed(readState.ClientInfo.Protocol);            
        }

        protected override void OnReadingFailed(Exception ex, ReadStateObject readState)
        {
            RemoveClientFromReceivers(readState.ClientInfo.ID);
        }

        protected override void OnSendingFailed(Exception ex, SendStateObject sendState)
        {
            RemoveClientFromReceivers(sendState.ClientInfo.ID);
        }

        protected virtual void SendMessageToClient(IMessage message, ClientInfo client)
        {
            if (((ServerProtocol)client.Protocol).CanSendMessageToClient(client.LastContact))
            {
                StartSending(message, client);
            }
            else
            {
                RemoveClientFromReceivers(client.ID);
            }
        }

        protected virtual void RemoveClientFromReceivers(int clientId)
        {
            if (clients.ContainsKey(clientId))
            {
                var client = clients[clientId];

                // 2 phase closing connection - sending message may also end up with error -> will get here
                if (client.Connected)
                {
                    StartSending(new ErrorMessage("Connection closed on server side."), client);

                    client.Connected = false;
                }
                else
                {
                    try
                    {
                        // Close stream in second phase to avoid disposing stream while still in use
                        client.Stream.Close(100);
                    }
                    catch (Exception) { /* Do nothing */}

                    clients.Remove(clientId);
                }
                
            }
        }

        public void Dispose()
        {
            foreach (var info in clients.Values)
            {
                try
                {
                    info.Stream.Close();
                }
                catch (Exception) { /* Do nothing */} 
            }

            if (server != null)
                server.Stop();
        }
    }
}
