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
    /// <summary>
    /// Handles server side of chat communication
    /// </summary>
    class Server : NetworkCommunicator, IDisposable
    {
        /// <summary>
        /// TcpListener that listens for messages from multiple clients
        /// </summary>
        private TcpListener server = null;

        /// <summary>
        /// Clients hashed under their ids
        /// </summary>
        Dictionary<int, ClientInfo> clients;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Server()
        {
            clients = new Dictionary<int, ClientInfo>();
        }
        
        /// <summary>
        /// Starts listening on address provided by configuration
        /// </summary>
        public virtual void Run() 
        {
            try
            {
                Int32 port = Configuration.Configuration.ServerPort;
                IPAddress address = GetIPAddress(Configuration.Configuration.ServerIpV4Address);

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
            // Remove closed clients from collection
            clients.Values.ToList().Where(c => !c.Connected).ToList().ForEach(c => clients.Remove(c.ID));

            // Sent to all clients
            foreach (var client in clients.Values)
            {
                SendMessageToClient(message, client);
            }
        }

        /// <summary>
        /// Starts infinite loop where waits for clients to connect
        /// </summary>
        protected virtual void Listen()
        {
            // Start server
            server.Start();
            int lastId = 1;

            // Enter the listening loop.
            while (true)
            {
                //server.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), null);

                var client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                // Get a stream object for reading and writing
                var clientInfo = new ClientInfo(client.GetStream(), lastId, new ServerProtocol(this, lastId));

                clients.Add(lastId, clientInfo);

                // Starts listening on streem from with client
                StartReading(clientInfo);

                ++lastId;
            }
        }

        /// <summary>
        /// When message read
        /// </summary>
        /// <param name="message">Message read</param>
        /// <param name="readState">Info about reading</param>
        protected override void OnReadFinished(IMessage message, ReadStateObject readState)
        {
            Console.WriteLine("Server received: >{0}< from {1}", message, readState.ClientInfo.ID);

            readState.ClientInfo.LastContact = DateTime.Now;
            
            try
            {
                message.GetProcessed(readState.ClientInfo.Protocol);
            }
            catch (Exception ex)
            {
                OnReadingFailed(ex, readState);
            }
        }

        /// <summary>
        /// When reading failed - disconnect client
        /// </summary>
        /// <param name="ex">Failure reason</param>
        /// <param name="readState">Info about reading</param>
        protected override void OnReadingFailed(Exception ex, ReadStateObject readState)
        {
            RemoveClientFromReceivers(readState.ClientInfo.ID);
        }

        /// <summary>
        /// When sending failed - disconnect client
        /// </summary>
        /// <param name="ex">Failure reason</param>
        /// <param name="sendState">Info about sending</param>
        protected override void OnSendingFailed(Exception ex, SendStateObject sendState)
        {
            RemoveClientFromReceivers(sendState.ClientInfo.ID);
        }

        /// <summary>
        /// Sends message to given client If sending is not allowed, disconnects from client
        /// </summary>
        /// <param name="message">Message to send to the client</param>
        /// <param name="client">Info about client</param>
        protected virtual void SendMessageToClient(IMessage message, ClientInfo client)
        {
            var canSend = client.Connected 
                && ((ServerProtocol)client.Protocol).CanSendMessageToClient(client.LastContact);
 
            if (canSend)
            {
                StartSending(message, client);
            }
            else
            {
                RemoveClientFromReceivers(client.ID);
            }

            // Mark client as disconnected when error message sent
            if (message is ErrorMessage)
                client.Connected = false;
        }

        /// <summary>
        /// Disconnects clients - two phases.
        /// 1) fist just mark client as disconnected
        /// 2) remove disconnected clients
        /// </summary>
        /// <param name="clientId">Client to disconnect</param>
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
