using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat.Configuration;
using Chat.Client.Messages;

namespace Chat.Client.Core.Protocol
{
    class ClientProtocol: ICommunicationProtocol
    {
        protected Client Client { get; set; }

        protected string AgreedVersion { get; set; }

        protected bool VersionQueried { get; set; }

        protected DateTime LastChatMessageSent { get; set; }

        protected System.Timers.Timer PingTimer { get; set; }

        public ClientProtocol(Client client)
        {
            Client = client;
        }

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

        public virtual void OnChatMessageSent()
        {
            LastChatMessageSent = DateTime.Now;
        }

        public virtual void ProcessMessage(Messages.AckMessage message)
        {
            Client.ProcessErrorMessage(new ErrorMessage(message.ToString()));
            StopPinging();
        }

        public virtual void ProcessMessage(Messages.ErrorMessage message)
        {
            Client.ProcessErrorMessage(message);
            StopPinging();
        }

        public virtual void ProcessMessage(Messages.HelloMessage message)
        {
            Client.ProcessErrorMessage(new ErrorMessage(message.ToString()));
            StopPinging();
        }

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

        public virtual void ProcessMessage(Messages.OllehMessage message)
        {
            System.Diagnostics.Debug.Assert(AgreedVersion == null, "Version already agreed");

            AgreedVersion = message.Version;
            StartPinging();

            Client.SendMessage(new AckMessage());
        }

        public virtual void ProcessMessage(Messages.PingMessage message)
        {
            Client.ProcessErrorMessage(new ErrorMessage(message.ToString()));
            StopPinging();
        }

        public virtual void ProcessMessage(Messages.PongMessage message)
        {
            if (!PingingAllowed())
            {
                Client.ProcessErrorMessage(new ErrorMessage(message.ToString()));
                StopPinging();
            }
        }

        protected virtual void StartPinging()
        {
            if (PingingAllowed() && PingTimer == null)
            {
                PingTimer = new System.Timers.Timer(60 * 1000);
                /* TODO:  
                PingTimer.Elapsed += new System.Timers.ElapsedEventHandler(delegate
                {
                    if (LastChatMessageSent.AddMinutes(1).CompareTo(DateTime.Now) < 1)
                        Client.SendMessage(new PingMessage());
                });
                */
                PingTimer.Start();
            }
        }

        protected virtual void StopPinging()
        {
            if (PingingAllowed() && PingTimer != null)
            {
                PingTimer.Stop();
            }
        }

        protected virtual bool PingingAllowed()
        {
            return AgreedVersion == Versions.VERSION_1_1;
        }
    }
}
