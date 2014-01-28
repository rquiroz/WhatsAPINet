using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using WhatsAppApi.Helper;

namespace WhatsAppApi
{
    /// <summary>
    /// Contains methods to connect to the Whatsapp network
    /// </summary>
    public class WhatsNetwork
    {
        /// <summary>
        /// The time between sending and recieving
        /// </summary>
        private readonly int recvTimeout;

        /// <summary>
        /// The hostname of the whatsapp server
        /// </summary>
        private readonly string whatsHost;

        /// <summary>
        /// The port of the whatsapp server
        /// </summary>
        private readonly int whatsPort;

        /// <summary>
        /// A list of bytes for incomplete messages
        /// </summary>
        private List<byte> incomplete_message = new List<byte>();

        /// <summary>
        /// A socket to connect to the whatsapp network
        /// </summary>
        private Socket socket;

        /// <summary>
        /// Default class constructor
        /// </summary>
        /// <param name="whatsHost">The hostname of the whatsapp server</param>
        /// <param name="port">The port of the whatsapp server</param>
        /// <param name="timeout">Timeout for the connection</param>
        public WhatsNetwork(string whatsHost, int port, int timeout = 2000)
        {
            this.recvTimeout = timeout;
            this.whatsHost = whatsHost;
            this.whatsPort = port;
            this.incomplete_message = new List<byte>();
        }

        /// <summary>
        /// Default class constructor
        /// </summary>
        /// <param name="timeout">Timeout for the connection</param>
        public WhatsNetwork(int timeout = 2000)
        {
            this.recvTimeout = timeout;
            this.whatsHost = Settings.WhatsConstants.WhatsAppHost;
            this.whatsPort = Settings.WhatsConstants.WhatsPort;
            this.incomplete_message = new List<byte>();
        }
        
        /// <summary>
        /// Connect to the whatsapp server
        /// </summary>
        public void Connect()
        {
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.Connect(this.whatsHost, this.whatsPort);
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, this.recvTimeout);
            this.socket.ReceiveBufferSize = Int32.MaxValue;
            //this.socket.SendBufferSize = Int32.MaxValue;

            if (!this.socket.Connected)
                throw new ConnectionException("Cannot connect");
        }

        /// <summary>
        /// Disconnect from the whatsapp server
        /// </summary>
        public void Disconenct()
        {
            if (this.socket != null)
            {
                this.socket.Close();
            }
        }

        /// <summary>
        /// Read 1024 bytes 
        /// </summary>
        /// <returns></returns>
        public byte[] ReadData(int length = 1024)
        {
            return Socket_read(length);
        }

        /// <summary>
        /// Send data to the whatsapp server
        /// </summary>
        /// <param name="data">Data to be send as a string</param>
        public void SendData(string data)
        {
            Socket_send(data, data.Length, 0);
        }

        /// <summary>
        /// Send data to the whatsapp server
        /// </summary>
        /// <param name="data">Data to be send as a byte array</param>
        public void SendData(byte[] data)
        {
            Socket_send(data);
        }

        public byte[] ReadNextNode()
        {
            byte[] nodeHeader = this.ReadData(3);
            if (nodeHeader.Length != 3)
            {
                throw new Exception("Failed to read node header");
            }
            int nodeLength = 0;
            nodeLength = (int)nodeHeader[1] << 8;
            nodeLength |= (int)nodeHeader[2] << 0;

            byte[] nodeData = this.ReadData(nodeLength);
            if (nodeData.Length != nodeLength)
            {
                throw new Exception("Read Next Tree error");
            }

            byte[] fullData = new byte[nodeHeader.Length + nodeData.Length];
            List<byte> buff = new List<byte>();
            buff.AddRange(nodeHeader);
            buff.AddRange(nodeData);
            return buff.ToArray();
        }
       
        /// <summary>
        /// Read in a message with a specific length
        /// </summary>
        /// <param name="length">The lengh of the message</param>
        /// <returns>The recieved data as a byte array</returns>
        private byte[] Socket_read(int length)
        {
            if (!socket.Connected)
            {
                throw new ConnectionException();
            }

            var buff = new byte[length];
            int receiveLength = 0;
            do
            {
                try
                {
                    receiveLength = socket.Receive(buff, 0, length, 0);
                }
                catch (SocketException excpt)
                {
                    if (excpt.SocketErrorCode == SocketError.TimedOut)
                    {
                        Console.WriteLine("Socket-Timout");
                        return null;
                    }
                    else
                    {
                        throw new ConnectionException("Unknown error occured", excpt);
                    }
                }
            } 
            while (receiveLength <= 0);

            byte[] tmpRet = new byte[receiveLength];
            if (receiveLength > 0)
                Buffer.BlockCopy(buff, 0, tmpRet, 0, receiveLength);
            return tmpRet;
        }

        /// <summary>
        /// Sends data of a specific length to the server
        /// </summary>
        /// <param name="data">The data that needs to be send</param>
        /// <param name="length">The lenght of the data</param>
        /// <param name="flags">Optional flags</param>
        private void Socket_send(string data, int length, int flags)
        {
            var tmpBytes = WhatsApp.SYSEncoding.GetBytes(data);
            this.socket.Send(tmpBytes);
        }

        /// <summary>
        /// Send data to the server
        /// </summary>
        /// <param name="data">The data that needs to be send as a byte array</param>
        private void Socket_send(byte[] data)
        {
            this.socket.Send(data);
        }

        /// <summary>
        /// Returns the socket status.
        /// </summary>
        public bool SocketStatus
        {
            get { return socket.Connected; }
        }
    }
}
