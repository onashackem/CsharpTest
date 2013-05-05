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
    class Client: NetworkCommunicator, IDisposable
    {
        protected TcpClient client = null;
        protected ClientInfo communicationInfo = null;

        public Dictionary<string, ClientProtocol> VersionedProtocols { get; protected set; }
        
        public string Name { get; set; }

        public event EventHandler<ChatMessageEventArgs> ChatMessageReceived;
        public event EventHandler<ErrorMessageEventArgs> ErrorMessageReceived;

        public Client()
        {
            VersionedProtocols = new Dictionary<string, ClientProtocol>();

            // Protocol handles both versions
            var protocol = new ClientProtocol(this);
            VersionedProtocols.Add(Versions.VERSION_1_0, protocol);
            VersionedProtocols.Add(Versions.VERSION_1_1, protocol);
        }

        public virtual bool TryConnect(string hostAddress)
        {
            if (Connected)
                throw new InvalidOperationException("Client is already connected.");

            try
            {
                var port = Configuration.Configuration.ServerPort;
                var address = GetIPAddress(hostAddress);

                if (address == null)
                    throw new InvalidOperationException("Invalid IP address: " + hostAddress);

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

        public virtual void SendMessage(IMessage message)
        {
            StartSending(message, communicationInfo);
            ((ClientProtocol)communicationInfo.Protocol).OnChatMessageSent();

            Console.WriteLine("Client {1} sent: >>{0}<<", message, Name);
        }

        public virtual void ChangeProtocol(string version)
        {
            System.Diagnostics.Debug.Assert(VersionedProtocols.ContainsKey(version), "Unavailable version");

            var protocol = VersionedProtocols[version];
            if (this.communicationInfo.Protocol != protocol)
                this.communicationInfo.Protocol = protocol;

            this.communicationInfo.Protocol = protocol;
        }

        public virtual void ProcessChatMessage(ChatMessage message)
        {
            if (ChatMessageReceived != null)
                ChatMessageReceived(this, new ChatMessageEventArgs() { Message = message.Message, User = message.From });
        }

        public virtual void ProcessErrorMessage(ErrorMessage message)
        {
            if (ErrorMessageReceived != null)
                ErrorMessageReceived(this, new ErrorMessageEventArgs() { Error = message.Error });

            communicationInfo.Stream.Close();
        }

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

        protected override void OnReadFinished(IMessage message, ReadStateObject readState)
        {
            Console.WriteLine("Client {1} received >>{0}<<", message, Name);

            System.Diagnostics.Debug.Assert(readState.ClientInfo == communicationInfo, "Should equal");

            message.GetProcessed(communicationInfo.Protocol);
        }

        protected override void OnReadingFailed(Exception ex, ReadStateObject readState)
        {
            if (communicationInfo != null && communicationInfo.Stream != null)
                communicationInfo.Stream.Close();            
        }

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
