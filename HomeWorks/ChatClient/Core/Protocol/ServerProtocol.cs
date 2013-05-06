using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Configuration;
using Chat.Client.Messages;

namespace Chat.Client.Core.Protocol
{
    /// <summary>
    /// Handles server side of chat communication - one instance per client
    /// </summary>
    class ServerProtocol : ICommunicationProtocol
    {
        /// <summary>
        /// Server that communicates
        /// </summary>
        protected Server.Server server;

        /// <summary>
        /// Client id on the other side of communication
        /// </summary>
        protected int ClientId { get; set; }

        /// <summary>
        /// Agreed version with client
        /// </summary>
        protected string AgreedVersion { get; set; }

        /// <summary>
        /// Version is confirmed
        /// </summary>
        protected bool VersionConfirmed { get; set; }

        /// <summary>
        /// Constructor with server and clientid
        /// </summary>
        /// <param name="server">Server</param>
        /// <param name="clientId">Client on other side</param>
        public ServerProtocol(Server.Server server, int clientId)
        {
            this.server = server;
            this.ClientId = clientId;
        }

        /// <summary>
        /// When AckMessage received - version verified
        /// </summary>
        /// <param name="message">AckMessage</param>
        public virtual void ProcessMessage(Messages.AckMessage message)
        {
            VersionConfirmed = true;
        }

        /// <summary>
        /// When ErrorMessage received 
        /// </summary>
        /// <param name="message">ErrorMessage</param>
        public virtual void ProcessMessage(Messages.ErrorMessage message)
        {
            server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
        }

        /// <summary>
        /// When HelloMessage received - choose best protocol version
        /// </summary>
        /// <param name="message">HelloMessage</param>
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

        /// <summary>
        /// When ChatMessage received - send to all other clients
        /// </summary>
        /// <param name="message">ChatMessage</param>
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

        /// <summary>
        /// When OllehMessage received - error
        /// </summary>
        /// <param name="message">OllehMessage</param>
        public virtual void ProcessMessage(Messages.OllehMessage message)
        {
            server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
        }

        /// <summary>
        /// When PingMessage received - remember the time
        /// </summary>
        /// <param name="message">PingMessage</param>
        public virtual void ProcessMessage(Messages.PingMessage message)
        {
            if (!PingingAllowed())
            {
                server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
                return;
            }

            server.SendMessage(new PongMessage(), ClientId);
        }

        /// <summary>
        /// When PongMessage received - error
        /// </summary>
        /// <param name="message">PongMessage</param>
        public virtual void ProcessMessage(Messages.PongMessage message)
        {
            // Unknown message
            server.SendMessage(new ErrorMessage(message.ToString()), ClientId);
        }

        /// <summary>
        /// Determines wheter client is available to receive.
        /// Unavailable when pinging is allowed and last contact from client was more than 3 minutes ago
        /// </summary>
        /// <param name="lastContact">Last contact of client</param>
        /// <returns>True when client id OK to receive, false if not</returns>
        public virtual bool CanSendMessageToClient(DateTime lastContact)
        {
            // If protocol disalows pinging or last contact was not older that 3 minutes
            return !(PingingAllowed() && lastContact.AddMinutes(3).CompareTo(DateTime.Now) < 1);                
        }

        /// <summary>
        /// Determines the best protocol version based on offered client versions
        /// </summary>
        /// <param name="versions">Offered client versions</param>
        /// <returns>The best version</returns>
        protected virtual string FindBestProtocolVersion(string[] versions)
        {
            if (versions.Contains(Versions.VERSION_1_1))
                return Versions.VERSION_1_1;

            if (versions.Contains(Versions.VERSION_1_0))
                return Versions.VERSION_1_0;

            return null;
        }

        /// <summary>
        /// Determines whether version allows pinging
        /// </summary>
        /// <returns></returns>
        protected virtual bool PingingAllowed()
        {
            return AgreedVersion == Versions.VERSION_1_1;
        }
    }
}
