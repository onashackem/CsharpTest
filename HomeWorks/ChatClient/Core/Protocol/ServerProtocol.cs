using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Configuration;
using Chat.Client.Messages;

namespace Chat.Client.Core.Protocol
{
    class ServerProtocol : ICommunicationProtocol
    {
        protected Server.Server server;

        protected int ClientId { get; set; }

        protected string AgreedVersion { get; set; }

        protected bool VersionConfirmed { get; set; }

        public ServerProtocol(Server.Server server, int clientId)
        {
            this.server = server;
            this.ClientId = clientId;
        }

        public virtual void ProcessMessage(Messages.AckMessage message)
        {
            VersionConfirmed = true;
        }

        public virtual void ProcessMessage(Messages.ErrorMessage message)
        {
            throw new NotImplementedException();
        }

        public virtual void ProcessMessage(Messages.HelloMessage message)
        {
            System.Diagnostics.Debug.Assert(!VersionConfirmed, "Version should not be agreed yet");

            if (VersionConfirmed)
            {
                server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
                return;
            }

            var version = FindBestProtocolVersion(message.AvailableVersions);

            if (version != null)
            {
                server.SendMessage(new OllehMessage(version), ClientId);
                AgreedVersion = version;
            }
            else
            {
                server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
            }
        }

        public virtual void ProcessMessage(Messages.ChatMessage message)
        {
            System.Diagnostics.Debug.Assert(VersionConfirmed, "Version should not be agreed yet");

            if (VersionConfirmed)
            {
                // Send message to everybody
                server.SendMessage(message);
            }
            else
            {
                server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
            }
        }

        public virtual void ProcessMessage(Messages.OllehMessage message)
        {
            server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
        }

        public virtual void ProcessMessage(Messages.PingMessage message)
        {
            if (!PingingAllowed())
            {
                server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
                return;
            }

            server.SendMessage(new PongMessage(), ClientId);
        }

        public virtual void ProcessMessage(Messages.PongMessage message)
        {
            // Unknown message
            server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
        }

        protected virtual string FindBestProtocolVersion(string[] versions)
        {
            if (versions.Contains(Versions.VERSION_1_1))
                return Versions.VERSION_1_1;

            if (versions.Contains(Versions.VERSION_1_0))
                return Versions.VERSION_1_0;

            return null;
        }

        protected virtual bool PingingAllowed()
        {
            return AgreedVersion == Versions.VERSION_1_1;
        }

        public virtual bool CanSendMessageToClient(DateTime lastContact)
        {
            // If protocol disalows pinging or last contact was not older that 3 minutes
            // TODO: 1 -> 3
            return !(PingingAllowed() && lastContact.AddMinutes(1).CompareTo(DateTime.Now) < 1);                
        }
    }
}
