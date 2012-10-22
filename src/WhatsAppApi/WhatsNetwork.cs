using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using WhatsAppApi.Helper;

namespace WhatsAppApi
{
    public class WhatsNetwork
    {
        private readonly Encoding sysEncoding;
        private readonly int recvTimeout;
        private readonly string whatsHost;
        private readonly int whatsPort;

        private string incomplete_message = "";
        private Socket socket;
        private BinTreeNodeWriter binWriter;

        public WhatsNetwork(string whatsHost, int port, Encoding encoding, int timeout = 2000)
        {
            this.sysEncoding = encoding;
            this.recvTimeout = timeout;
            this.whatsHost = whatsHost;
            this.whatsPort = port;
            this.binWriter = new BinTreeNodeWriter(DecodeHelper.getDictionary());
        }

        public void Connect()
        {
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.Connect(this.whatsHost, this.whatsPort);
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, this.recvTimeout);
            //var tmpNetStream = new NetworkStream(this.socket);
            //this.streamReader = new StreamReader(tmpNetStream);
            //this.streamWriter = new StreamWriter(tmpNetStream);
        }

        public string ReadData()
        {
            string buff = "";
            string ret = Socket_read(1024);
            if (ret != null)
            {
                buff = this.incomplete_message + ret;
                this.incomplete_message = "";
            }
            return buff;
        }

        public void SendData(string data)
        {
            Socket_send(data, data.Length, 0);
        }

        public void SendNode(ProtocolTreeNode node)
        {
            //this.DebugPrint(node.NodeString("SENT: "));
            this.SendData(this.binWriter.Write(node));
        }

        private string Socket_read(int length)
        {
            var buff = new byte[length];
            try
            {
                socket.Receive(buff, 0, length, 0);
            }
            catch (SocketException excpt)
            {
                //if (excpt.SocketErrorCode == SocketError.TimedOut)
                //    Console.WriteLine("Socket-Timout");
                return null;
                //else
                //    Console.WriteLine("Unbehandelter Fehler bei Sockerread: {0}", excpt);
            }
            string tmpRet = this.sysEncoding.GetString(buff);
            return tmpRet;
        }

        private void Socket_send(string data, int length, int flags)
        {
            var tmpBytes = this.sysEncoding.GetBytes(data);
            this.socket.Send(tmpBytes);
        }
    }
}
