using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using ChatClient.Core;
using System.Net;
using Chat.Client.Messages;
using Chat.Client.Core;
using Chat.Client.Core.Protocol;
using Chat.Configuration;

namespace Chat.Client
{
    /// <summary>
    /// Chat client that communicates with chat server
    /// </summary>
    class Client: NetworkCommunicator, IDisposable
    {
        /// <summary>
        /// TCPClient that handles communication
        /// </summary>
        protected TcpClient client = null;

        /// <summary>
        /// Information about connection
        /// </summary>
        protected ClientInfo communicationInfo = null;

        /// <summary>
        /// Versionf of chat and protocols that handles the comunication rules
        /// </summary>
        public Dictionary<string, ClientProtocol> VersionedProtocols { get; protected set; }
        
        /// <summary>
        /// Internal name of client
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Event raised when client received a chat message from server
        /// </summary>
        public event EventHandler<ChatMessageEventArgs> ChatMessageReceived;

        /// <summary>
        /// Event raised when client received an error message from cerver
        /// </summary>
        public event EventHandler<ErrorMessageEventArgs> ErrorMessageReceived;

        /// <summary>
        /// Defualt construcotr, inits version dictionary
        /// </summary>
        public Client()
        {
            VersionedProtocols = new Dictionary<string, ClientProtocol>();

            // Protocol handles both versions
            var protocol = new ClientProtocol(this);
            VersionedProtocols.Add(Versions.VERSION_1_0, protocol);
            VersionedProtocols.Add(Versions.VERSION_1_1, protocol);
        }

        /// <summary>
        /// Attempts to connect to server
        /// </summary>
        /// <param name="hostAddress">Address or host name of server</param>
        /// <returns>Returns true if conection succeeded, false otherwise</returns>
        public virtual bool TryConnect(string hostAddress)
        {
            if (Connected)
                throw new InvalidOperationException("Client is already connected.");

            try
            {
                var port = Configuration.Configuration.ServerPort;
                var address = GetIPAddress(hostAddress);

                TcpClient client = new TcpClient(address.AddressFamily);
                client.Connect(address, port);

                var protocol = VersionedProtocols[Versions.VERSION_1_0];

                // Start with simple base protocol
                communicationInfo = new ClientInfo(client.GetStream(), protocol);

                StartReading(communicationInfo);

                protocol.IntroduceClient();

                Connected = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect: {0}\n{1}", ex.Message, ex.StackTrace);
            }

            return false;
        }

        /// <summary>
        /// Sends message to server
        /// </summary>
        /// <param name="message"></param>
        public virtual void SendMessage(IMessage message)
        {
            StartSending(message, communicationInfo);
            ((ClientProtocol)communicationInfo.Protocol).OnMessageSent();

            Console.WriteLine("Client {1} sent: >>{0}<<", message, Name);
        }

        /// <summary>
        /// Changes communication protocol based on server choice
        /// </summary>
        /// <param name="version"></param>
        public virtual void ChangeProtocol(string version)
        {
            System.Diagnostics.Debug.Assert(VersionedProtocols.ContainsKey(version), "Unavailable version");

            var protocol = VersionedProtocols[version];
            if (this.communicationInfo.Protocol != protocol)
                this.communicationInfo.Protocol = protocol;

            this.communicationInfo.Protocol = protocol;
        }

        /// <summary>
        /// Called when Chat message received
        /// </summary>
        /// <param name="message">Chat message</param>
        public virtual void ProcessChatMessage(ChatMessage message)
        {
            if (ChatMessageReceived != null)
                ChatMessageReceived(this, new ChatMessageEventArgs() { Message = message.Message, User = message.From });
        }

        /// <summary>
        /// Called when Error message received
        /// </summary>
        /// <param name="message">Error message</param>
        public virtual void ProcessErrorMessage(ErrorMessage message)
        {
            if (ErrorMessageReceived != null)
                ErrorMessageReceived(this, new ErrorMessageEventArgs() { Error = message.Error });

            communicationInfo.Stream.Close();
        }

        /// <summary>
        /// Disconnects open channels
        /// </summary>
        public virtual void Disconnect()
        {
            try
            {
                if (communicationInfo != null && communicationInfo.Stream != null)
                    communicationInfo.Stream.Close();

                if (client != null)
                    client.Close();
            }
            catch (Exception) { /* Nothing to do */}
        }

        /// <summary>
        /// Called when a meesage from server is received
        /// </summary>
        /// <param name="message">Message from server</param>
        /// <param name="readState">Read information data</param>
        protected override void OnReadFinished(IMessage message, ReadStateObject readState)
        {
            Console.WriteLine("Client {1} received >>{0}<<", message, Name);

            System.Diagnostics.Debug.Assert(readState.ClientInfo == communicationInfo, "Should equal");

            message.GetProcessed(communicationInfo.Protocol);
        }

        /// <summary>
        /// Called when reading message from server failed
        /// </summary>
        /// <param name="ex">Occured exception</param>
        /// <param name="readState">Read information data</param>
        protected override void OnReadingFailed(Exception ex, ReadStateObject readState)
        {
            if (communicationInfo != null && communicationInfo.Stream != null)
                communicationInfo.Stream.Close();            
        }

        /// <summary>
        /// Called when sending data to server failed
        /// </summary>
        /// <param name="ex">Occured exception</param>
        /// <param name="sendState">Send information data</param>
        protected override void OnSendingFailed(Exception ex, SendStateObject sendState)
        {
            if (communicationInfo != null && communicationInfo.Stream != null)
                communicationInfo.Stream.Close();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
