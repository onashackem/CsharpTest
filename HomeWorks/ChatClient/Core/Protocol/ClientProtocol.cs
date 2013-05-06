using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Configuration;
using Chat.Client.Messages;

namespace Chat.Client.Core.Protocol
{
    /// <summary>
    /// Base protocol that handles client side of communication (Versions 1.0 and 1.1)
    /// </summary>
    class ClientProtocol: ICommunicationProtocol
    {
        /// <summary>
        /// Client that communicates
        /// </summary>
        protected Client Client { get; set; }

        /// <summary>
        /// Agreed communication protocol version with server
        /// </summary>
        protected string AgreedVersion { get; set; }

        /// <summary>
        /// Was verison already queried to server?
        /// </summary>
        protected bool VersionQueried { get; set; }

        /// <summary>
        /// When was the last message sent to server
        /// </summary>
        protected DateTime LastChatMessageSent { get; set; }

        /// <summary>
        /// Timer that sends PingMessages to server
        /// </summary>
        protected System.Timers.Timer PingTimer { get; set; }

        /// <summary>
        /// Constructor with client
        /// </summary>
        /// <param name="client"></param>
        public ClientProtocol(Client client)
        {
            Client = client;
        }

        /// <summary>
        /// Clients send available protocol versions to client
        /// </summary>
        public virtual void IntroduceClient()
        {
            if (VersionQueried)
                throw new InvalidOperationException("Client has already introduced himself.");

            StringBuilder versions = new StringBuilder();

            foreach (var version in Client.VersionedProtocols.Keys)
            {
                versions.AppendFormat("{0} ", version);
            }

            Client.SendMessage(new HelloMessage(versions.ToString()));

            VersionQueried = true;
        }

        /// <summary>
        /// Called after client sent any message to client
        /// </summary>
        public virtual void OnMessageSent()
        {
            LastChatMessageSent = DateTime.Now;
        }

        /// <summary>
        /// When AckMessage received - error
        /// </summary>
        /// <param name="message">AckMessage</param>
        public virtual void ProcessMessage(Messages.AckMessage message)
        {
            Client.ProcessErrorMessage(new ErrorMessage(message.ToString()));
            StopPinging();
        }

        /// <summary>
        /// When ErrorMessage received
        /// </summary>
        /// <param name="message">ErrorMessage</param>
        public virtual void ProcessMessage(Messages.ErrorMessage message)
        {
            Client.ProcessErrorMessage(message);
            StopPinging();
        }

        /// <summary>
        /// When HelloMessage received - error
        /// </summary>
        /// <param name="message">HelloMessage</param>
        public virtual void ProcessMessage(Messages.HelloMessage message)
        {
            Client.ProcessErrorMessage(new ErrorMessage(message.ToString()));
            StopPinging();
        }

        /// <summary>
        /// Wen ChatMessage received - notifies ClientWindow
        /// </summary>
        /// <param name="message">ChatMessage </param>
        public virtual void ProcessMessage(Messages.ChatMessage message)
        {
            if (AgreedVersion == null)
            {
                Client.ProcessErrorMessage(new ErrorMessage(message.ToString()));
                StopPinging();
                return;
            }

            Client.ProcessChatMessage(message);
        }

        /// <summary>
        /// When OllehMessage received - sets protocol version
        /// </summary>
        /// <param name="message">OllehMessage with protocol version</param>
        public virtual void ProcessMessage(Messages.OllehMessage message)
        {
            System.Diagnostics.Debug.Assert(AgreedVersion == null, "Version already agreed");

            AgreedVersion = message.Version;
            StartPinging();

            Client.SendMessage(new AckMessage());
        }

        /// <summary>
        /// When PingMessage received - error
        /// </summary>
        /// <param name="message">PingMessage</param>
        public virtual void ProcessMessage(Messages.PingMessage message)
        {
            Client.ProcessErrorMessage(new ErrorMessage(message.ToString()));
            StopPinging();
        }

        /// <summary>
        /// When PongMessage received
        /// </summary>
        /// <param name="message">PongMessage</param>
        public virtual void ProcessMessage(Messages.PongMessage message)
        {
            if (!PingingAllowed())
            {
                Client.ProcessErrorMessage(new ErrorMessage(message.ToString()));
                StopPinging();
            }
        }

        /// <summary>
        /// When protocol allows pinging, pingns starts
        /// </summary>
        protected virtual void StartPinging()
        {
            if (PingingAllowed() && PingTimer == null)
            {
                PingTimer = new System.Timers.Timer(60 * 1000);
                PingTimer.Elapsed += new System.Timers.ElapsedEventHandler(delegate
                {
                    if (LastChatMessageSent.AddMinutes(1).CompareTo(DateTime.Now) < 1)
                        Client.SendMessage(new PingMessage());
                });

                PingTimer.Start();
            }
        }

        /// <summary>
        /// Stops pinging timer
        /// </summary>
        protected virtual void StopPinging()
        {
            if (PingingAllowed() && PingTimer != null)
            {
                PingTimer.Stop();
                PingTimer = null;
            }
        }

        /// <summary>
        /// Determines whether pinging is allowed in protocol version
        /// </summary>
        /// <returns></returns>
        protected virtual bool PingingAllowed()
        {
            return AgreedVersion == Versions.VERSION_1_1;
        }
    }
}
