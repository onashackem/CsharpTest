﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Chat.Client.Core;
using Chat.Client.Messages;
using System.Net;

namespace ChatClient.Core
{
    /// <summary>
    /// Class that defines basics of network communication - asynchronous data sending and receiving.
    /// </summary>
    abstract class NetworkCommunicator
    {
        /// <summary>
        /// Is communicator conected?
        /// </summary>
        public bool Connected { get; protected set; }

        /// <summary>
        /// Parses received messages
        /// </summary>
        protected MessageParser messageParser = new MessageParser();

        /// <summary>
        /// Tries to parse supplied IP address
        /// </summary>
        /// <param name="addressOrHostName">IP address or host name</param>
        /// <returns>Return IPaddress (IPv6 preffered) or throws error when filed to parse</returns>
        protected IPAddress GetIPAddress(string addressOrHostName)
        {
            var hostInfo = Dns.GetHostEntry(addressOrHostName);

            if (hostInfo.AddressList == null ||hostInfo.AddressList.Count() == 0)
                throw new InvalidOperationException("Invalid IP address: " + addressOrHostName);

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

        /// <summary>
        /// Starts listening on network stream
        /// </summary>
        /// <param name="streamInfo">Info about stream</param>
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

        /// <summary>
        /// Called when reading any mesage from stream finished
        /// </summary>
        /// <param name="result">Information about reading</param>
        protected void ReadCallback(IAsyncResult result)
        {
            var state = (ReadStateObject)result.AsyncState;
            var stream = state.ClientInfo.Stream;

            try
            {
                // End reading
                var bytesRead = stream.EndRead(result);

                // Proces read data
                state.ProcessReadData();

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

        /// <summary>
        /// Sends message to the stream other side
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="streamInfo">Information about stream</param>
        protected void StartSending(IMessage message, ClientInfo streamInfo)
        {
            Console.WriteLine("Sending >{0}< to {1}", message, streamInfo.ID);

            var data = message.ToString();

            System.Diagnostics.Debug.Assert(data.EndsWith("\n"), "Message has to end with a newline!");

            // Create state object
            var state = new SendStateObject(streamInfo, data);
            var buffer = state.Buffer;

            System.Diagnostics.Debug.Assert(streamInfo.Connected, "Client should be connected to receive messae.");

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

        /// <summary>
        /// Called when sending finished
        /// </summary>
        /// <param name="result">Sending result</param>
        protected void SendCallback(IAsyncResult result)
        {
            var state = (SendStateObject)result.AsyncState;
            var stream = state.ClientInfo.Stream;

            stream.EndWrite(result);
        }

        /// <summary>
        /// Called when message reading finished
        /// </summary>
        /// <param name="message">Received message</param>
        /// <param name="readState">Information about reading</param>
        protected abstract void OnReadFinished(IMessage message, ReadStateObject readState);

        /// <summary>
        /// Called when reading failed
        /// </summary>
        /// <param name="ex">Failure reason</param>
        /// <param name="readState">Information about reading</param>
        protected abstract void OnReadingFailed(Exception ex, ReadStateObject readState);

        /// <summary>
        /// Called when sending failed
        /// </summary>
        /// <param name="ex">Failure reason</param>
        /// <param name="sendState">Information about sending</param>
        protected abstract void OnSendingFailed(Exception ex, SendStateObject sendState);

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
            public int BufferSize { get { return 100; } }

            /// <summary>
            /// Buffer for reading data
            /// </summary>
            public byte[] Buffer { get; private set; }

            /// <summary>
            /// Represents the whole data
            /// </summary>
            public string Data { get { return encoding.GetString(readBytes.ToArray()).Replace("\0", ""); } }

            private readonly List<byte> readBytes;

            // Communication encoding
            private Encoding encoding = Encoding.UTF8;

            /// <summary>
            /// Constructor with client information
            /// </summary>
            /// <param name="info">Client information</param>
            public ReadStateObject(ClientInfo info)
            {
                ClientInfo = info;
                readBytes = new List<byte>(BufferSize);
                Buffer = new byte[BufferSize];
            }

            /// <summary>
            /// Called after data are read
            /// </summary>
            public void ProcessReadData()
            {
                readBytes.AddRange(Buffer);
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

            /// <summary>
            /// Construstor with sending data and client information
            /// </summary>
            /// <param name="info">Client information</param>
            /// <param name="data">Data to send</param>
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
            /// <summary>
            /// Network stream for communicaton
            /// </summary>
            public NetworkStream Stream { get; private set; }

            /// <summary>
            /// Protocol of communication
            /// </summary>
            public ICommunicationProtocol Protocol { get; set; }

            /// <summary>
            /// Last contact from client
            /// </summary>
            public DateTime LastContact { get; set; }

            /// <summary>
            /// Client id
            /// </summary>
            public int ID { get; private set; }

            /// <summary>
            /// Is client still connected
            /// </summary>
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
