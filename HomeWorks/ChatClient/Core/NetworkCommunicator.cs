using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Chat.Client.Core;
using Chat.Client.Messages;

namespace ChatClient.Core
{
    abstract class NetworkCommunicator
    {
        public bool Connected { get; protected set; }

        protected MessageParser messageParser = new MessageParser();

        protected abstract void OnReadFinished(IMessage message, ReadStateObject readState);
        protected abstract void OnReadingFailed(Exception ex, ReadStateObject readState);
        protected abstract void OnSendingFailed(Exception ex, SendStateObject sendState);

        protected void StartReading(NetworkStreamInfo streamInfo)
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
            var stream = state.StreamInfo.Stream;

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
                    object message = messageParser.ParseMessage(state.Data);
                    OnReadFinished(message, state);

                    // New reading starts
                    state = new ReadStateObject(state.StreamInfo);
                }

                // Read next message
                stream.BeginRead(state.Buffer, 0, state.BufferSize, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception ex)
            {
                OnReadingFailed(ex, state);
            }
        }

        protected void StartSending(String message, NetworkStreamInfo streamInfo)
        {
            System.Diagnostics.Debug.Assert(message.EndsWith("\n"), "Message has to end with a newline!");

            // Create state object
            var state = new SendStateObject(streamInfo, message);
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
            var stream = state.StreamInfo.Stream;

            stream.EndWrite(result);
        }

        /// <summary>
        /// Class that is send as ReadCallback async state
        /// </summary>
        protected class ReadStateObject
        {
            /// <summary>
            /// Opened socket
            /// </summary>
            public NetworkStreamInfo StreamInfo { get; private set; }

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

            public ReadStateObject(NetworkStreamInfo stream)
            {
                StreamInfo = stream;
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
            /// Opened socket
            /// </summary>
            public NetworkStreamInfo StreamInfo { get; private set; }

            /// <summary>
            /// Gets decoded data to bytes
            /// </summary>
            public byte[] Buffer { get { return encoding.GetBytes(data); } }

            public SendStateObject(NetworkStreamInfo stream, string data)
            {
                StreamInfo = stream;
                this.data = data;
            }
        }

        /// <summary>
        /// Wrapper around NetworkStream - to remember its last contact
        /// </summary>
        protected class NetworkStreamInfo
        {
            public NetworkStream Stream { get; private set; }

            public DateTime LastContact { get; set; }

            public int ID { get; private set; }

            /// <summary>
            /// Constructor for server - to distinguis streams by added id
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="id"></param>
            public NetworkStreamInfo(NetworkStream stream, int id)
            {
                Stream = stream;
                ID = id;
            }

            /// <summary>
            /// Constructor for clients - no id needed
            /// </summary>
            /// <param name="stream"></param>
            public NetworkStreamInfo(NetworkStream stream) : this (stream, 1) {}
        }
    }
}
