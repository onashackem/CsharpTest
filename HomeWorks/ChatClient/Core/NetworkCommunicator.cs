using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Chat.Client.Core;
using Chat.Client.Messages;
using System.Net;

namespace ChatClient.Core
{
    abstract class NetworkCommunicator
    {
        public bool Connected { get; protected set; }

        protected MessageParser messageParser = new MessageParser();

        protected abstract void OnReadFinished(IMessage message, ReadStateObject readState);
        protected abstract void OnReadingFailed(Exception ex, ReadStateObject readState);
        protected abstract void OnSendingFailed(Exception ex, SendStateObject sendState);

        protected IPAddress GetIPAddress(string addressOrHostName)
        {
            var hostInfo = Dns.GetHostEntry(addressOrHostName);

            if (hostInfo.AddressList.Count() == 0)
                return null;


            var address = hostInfo.AddressList[0];

            /* TODO: IPv4 to IPv6 mapping
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
                return address;

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                // Try to map to IPv6 address
                return IPAddress.Parse(String.Format("::FFFF:{0}", address));
            }
            */

            return address;
        }

        protected void StartReading(ClientInfo streamInfo)
        {
            ReadStateObject state = new ReadStateObject(streamInfo);

            try
            {
                streamInfo.Stream.BeginRead(state.Buffer, 0, state.BufferSize, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception ex)
            {
                OnReadingFailed(ex, state);
            }
        }

        protected void ReadCallback(IAsyncResult result)
        {
            var state = (ReadStateObject)result.AsyncState;
            var stream = state.ClientInfo.Stream;

            try
            {
                // End reading
                var bytesRead = stream.EndRead(result);

                // Proces read data
                state.DecodeBuffer();

                // If whole message read
                if (state.Data.EndsWith("\n"))
                {
                    // Let communicator handle itself
                    var message = messageParser.ParseMessage(state.Data);
                    OnReadFinished(message, state);

                    // New reading starts
                    state = new ReadStateObject(state.ClientInfo);
                }

                // Read next message
                stream.BeginRead(state.Buffer, 0, state.BufferSize, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception ex)
            {
                OnReadingFailed(ex, state);
            }
        }

        protected void StartSending(IMessage message, ClientInfo streamInfo)
        {
            var data = message.ToString();

            System.Diagnostics.Debug.Assert(data.EndsWith("\n"), "Message has to end with a newline!");

            // Create state object
            var state = new SendStateObject(streamInfo, data);
            var buffer = state.Buffer;

            try
            {
                // Send asynchronously
                streamInfo.Stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(SendCallback), state);
            }
            catch (Exception ex)
            {
                OnSendingFailed(ex, state);
            }
        }

        protected void SendCallback(IAsyncResult result)
        {
            var state = (SendStateObject)result.AsyncState;
            var stream = state.ClientInfo.Stream;

            stream.EndWrite(result);
        }

        /// <summary>
        /// Class that is send as ReadCallback async state
        /// </summary>
        protected class ReadStateObject
        {
            /// <summary>
            /// Opened socket information
            /// </summary>
            public ClientInfo ClientInfo { get; private set; }

            /// <summary>
            /// Size of Buffer
            /// </summary>
            public int BufferSize { get { return 1024; } }

            /// <summary>
            /// Buffer for reading data
            /// </summary>
            public byte[] Buffer { get; private set; }

            /// <summary>
            /// Represents the whole data
            /// </summary>
            public string Data { get { return sb.ToString(); } }

            // Received data string.
            private StringBuilder sb = new StringBuilder();

            // Communication encoding
            private Encoding encoding = Encoding.UTF8;

            public ReadStateObject(ClientInfo info)
            {
                ClientInfo = info;
                Init();
            }

            public void DecodeBuffer()
            {
                sb.Append(encoding.GetString(Buffer).Replace("\0", ""));
                Init();
            }

            private void Init()
            {
                Buffer = new byte[BufferSize];
            }
        }

        /// <summary>
        /// Class that is send as SendCallback async state
        /// </summary>
        protected class SendStateObject
        {
            private string data;

            // Communication encoding
            private Encoding encoding = Encoding.UTF8;

            /// <summary>
            /// Opened socket information
            /// </summary>
            public ClientInfo ClientInfo { get; private set; }

            /// <summary>
            /// Gets decoded data to bytes
            /// </summary>
            public byte[] Buffer { get { return encoding.GetBytes(data); } }

            public SendStateObject(ClientInfo info, string data)
            {
                ClientInfo = info;
                this.data = data;
            }
        }

        /// <summary>
        /// Wrapper around NetworkStream - to remember its last contact
        /// </summary>
        protected class ClientInfo
        {
            public NetworkStream Stream { get; private set; }

            public ICommunicationProtocol Protocol { get; set; }

            public DateTime LastContact { get; set; }

            public int ID { get; private set; }

            public bool Connected { get; set; }

            /// <summary>
            /// Constructor for server - to distinguis streams by added id
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="id"></param>
            public ClientInfo(NetworkStream stream, int id, ICommunicationProtocol protocol)
            {
                Stream = stream;
                ID = id;
                Protocol = protocol;
                Connected = true;
            }

            /// <summary>
            /// Constructor for clients - no id needed
            /// </summary>
            /// <param name="stream"></param>
            public ClientInfo(NetworkStream stream, ICommunicationProtocol protocol) 
                : this(stream, 1, protocol) 
            { 
            }
        }
    }
}
