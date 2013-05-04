using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace ChatClient.Core
{
    abstract class NetworkCommunicator
    {
        protected abstract void OnReadFinished(string data, NetworkStream stream);
        protected abstract void OnReadingFailed(Exception ex, NetworkStream stream, ReadStateObject state);
        protected abstract void OnSendingFailed(Exception ex, NetworkStream stream, SendStateObject state);

        protected void StartReading(NetworkStream stream)
        {
            ReadStateObject state = new ReadStateObject(stream);

            try
            {
                stream.BeginRead(state.Buffer, 0, state.BufferSize, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception ex)
            {
                OnReadingFailed(ex, stream, state);
            }
        }

        protected void ReadCallback(IAsyncResult result)
        {
            var state = (ReadStateObject)result.AsyncState;
            var stream = state.Stream;

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
                    OnReadFinished(state.Data, stream);
                }

                // Read next message
                stream.BeginRead(state.Buffer, 0, state.BufferSize, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception ex)
            {
                OnReadingFailed(ex, stream, state);
            }
        }

        protected void StartSending(String message, NetworkStream stream)
        {
            System.Diagnostics.Debug.Assert(message.EndsWith("\n"), "Message has to end with a newline!");

            // Create state object
            var state = new SendStateObject(stream, message);
            var buffer = state.Buffer;

            try
            {
                // Send asynchronously
                stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(SendCallback), state);
            }
            catch (Exception ex)
            {
                OnSendingFailed(ex, stream, state);
            }
        }

        protected void SendCallback(IAsyncResult result)
        {
            var state = (SendStateObject)result.AsyncState;
            var stream = state.Stream;

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
            public NetworkStream Stream { get; private set; }

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

            public ReadStateObject(NetworkStream stream)
            {
                Stream = stream;
                ClearBuffer();
            }

            public void DecodeBuffer()
            {
                sb.Append(encoding.GetString(Buffer).Replace("\0", ""));
                ClearBuffer();
            }

            private void ClearBuffer()
            {
                Buffer = new byte[BufferSize];
            }
        }

        /// <summary>
        /// Class that is send as ReadCallback async state
        /// </summary>
        protected class SendStateObject
        {
            /// <summary>
            /// Opened socket
            /// </summary>
            public NetworkStream Stream { get; private set; }

            /// <summary>
            /// Represents the whole data
            /// </summary>
            public string Data { get; private set; }

            public byte[] Buffer { get { return encoding.GetBytes(Data); } }

            // Communication encoding
            private Encoding encoding = Encoding.UTF8;

            public SendStateObject(NetworkStream stream, string data)
            {
                Stream = stream;
                Data = data;
            }
        }
    }
}
