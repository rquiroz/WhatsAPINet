using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    internal class BinTreeNodeWriter
    {
        //private string output;
        private List<byte> buffer;
        private Dictionary<string, int> tokenMap;

        //change to protocol 1.2
        public byte[] Encryptionkey { get; set; }

        public BinTreeNodeWriter(string[] dict)
        {
            this.tokenMap = new Dictionary<string, int>();
            for (int i = 0; i < dict.Length; i++)
            {
                if (dict[i] != null && dict[i].Length > 0)
                {
                    if (!this.tokenMap.ContainsKey(dict[i]))
                        this.tokenMap.Add(dict[i], i);
                    else
                        this.tokenMap[dict[i]] = i;
                }
            }

            buffer = new List<byte>();
        }

        //public string StartStream(string domain, string resource)
        //{
        //    var attributes = new List<KeyValue>();
        //    this.output = "WA";
        //    //this.output += "\x01" + "\x01" + "\x00" + "\x19";

        //    attributes.Add(new KeyValue("to", domain));
        //    attributes.Add(new KeyValue("resource", resource));
        //    this.writeListStart(attributes.Count*2 + 1);

        //    this.output += "\x01";
        //    this.writeAttributes(attributes.ToArray());
        //    string ret = this.output;
        //    this.output = "";
        //    return ret;
        //}

        public byte[] StartStream(string domain, string resource)
        {
            var attributes = new List<KeyValue>();
            // protocol 1.2
            this.buffer = new List<byte>();
            
            attributes.Add(new KeyValue("to", domain));
            attributes.Add(new KeyValue("resource", resource));
            this.writeListStart(attributes.Count * 2 + 1);

            this.buffer.Add(1);
            this.writeAttributes(attributes.ToArray());

            byte[] ret = this.flushBuffer();
            this.buffer.Add((byte)'W');
            this.buffer.Add((byte)'A');
            this.buffer.Add(0x1);
            this.buffer.Add(0x2);
            this.buffer.AddRange(ret);
            ret = buffer.ToArray();
            this.buffer = new List<byte>();
            return ret;
        }

        //public string Write(ProtocolTreeNode node)
        //{
        //    if (node == null)
        //    {
        //        this.output += "\x00";
        //    }
        //    else
        //    {
        //        this.writeInternal(node);
        //    }
        //    return this.flushBuffer();
        //}
        public byte[] Write(ProtocolTreeNode node, bool encrypt = true)
        {
            if (node == null)
            {
                this.buffer.Add(0);
            }
            else
            {
                this.DebugPrint(node.NodeString("SENT: "));
                this.writeInternal(node);
            }
            return this.flushBuffer(encrypt);
        }

        //protected string flushBuffer()
        //{
        //    int size = this.output.Length;
        //     this.output = this.GetInt16(size) + this.output;
        //    string ret = this.output;
        //    this.output = "";
        //    return ret;
        //}

        //change to protocol 1.2
        protected byte[] flushBuffer(bool encrypt = true)
        {
            byte[] data = this.buffer.ToArray();
            byte[] size = this.GetInt24(data.Length);

            if (encrypt && this.Encryptionkey != null)
            {
                data = Encryption.WhatsappEncrypt(Encryptionkey, data, true);
                size[0] |= 0x8;
            }
            byte[] ret = new byte[data.Length + 3];
            Buffer.BlockCopy(size, 0, ret, 0, 3);
            Buffer.BlockCopy(data, 0, ret,3, data.Length);
            this.buffer = new List<byte>();
            return ret;
        }

        //protected void writeAttributes(IEnumerable<KeyValue> attributes)
        //{
        //    if (attributes != null)
        //    {
        //        foreach (var item in attributes)
        //        {
        //            this.writeString(item.Key);
        //            this.writeString(item.Value);
        //        }
        //    }
        //}
        protected void writeAttributes(IEnumerable<KeyValue> attributes)
        {
            if (attributes != null)
            {
                foreach (var item in attributes)
                {
                    this.writeString(item.Key);
                    this.writeString(item.Value);
                }
            }
        }

        //private string GetInt16(int len)
        //{
        //    string ret = chr((len & 0xff00) >> 8);
        //    ret += chr(len & 0x00ff);
        //    return ret;
        //}
        private byte[] GetInt16(int len)
        {
            byte[] ret = new byte[2];
            ret[0] = (byte)((len & 0xff00) >> 8);
            ret[1] = (byte)(len & 0x00ff);
            return ret;
        }

        //private string GetInt24(int len)
        //{
        //    string ret = chr((len & 0xf0000) >> 16);
        //    ret += chr((len & 0xff00) >> 8);
        //    ret += chr(len & 0x00ff);
        //    return ret;
        //}
        private byte[] GetInt24(int len)
        {
            byte[] ret = new byte[3];
            ret[0] = (byte)((len & 0xf0000) >> 16);
            ret[1] = (byte)((len & 0xff00) >> 8);
            ret[2] = (byte)(len & 0xff);
            return ret;
        }

        //protected void writeBytes(string bytes)
        //{
        //    int len = bytes.Length;
        //    if (len >= 0x100)
        //    {
        //        this.output += "\xfd";
        //        this.writeInt24(len);
        //    }
        //    else
        //    {
        //        this.output += "\xfc";
        //        this.writeInt8(len);
        //    }
        //    this.output += bytes;
        //}
        protected void writeBytes(string bytes)
        {
            writeBytes(WhatsApp.SYSEncoding.GetBytes(bytes));
        }
        protected void writeBytes(byte[] bytes)
        {
            int len = bytes.Length;
            if (len >= 0x100)
            {
                this.buffer.Add(0xfd);
                this.writeInt24(len);
            }
            else
            {
                this.buffer.Add(0xfc);
                this.writeInt8(len);
            }
            this.buffer.AddRange(bytes);
        }

        //protected void writeInt16(int v)
        //{
        //    //string ret = "";
        //    this.output += chr((v & 0xff00) >> 8);
        //    this.output += chr((v & 0x00ff) >> 0);
        //    //this.output = ret + this.output;
        //}
        protected void writeInt16(int v)
        {
            this.buffer.Add((byte)((v & 0xff00) >> 8));
            this.buffer.Add((byte)(v & 0x00ff));
        }

        //protected void writeInt24(int v)
        //{
        //    this.output += chr((v & 0xff0000) >> 16);
        //    this.output += chr((v & 0x00ff00) >> 8);
        //    this.output += chr((v & 0x0000ff) >> 0);
        //}
        protected void writeInt24(int v)
        {
            this.buffer.Add((byte)((v & 0xff0000) >> 16));
            this.buffer.Add((byte)((v & 0x00ff00) >> 8));
            this.buffer.Add((byte)(v & 0x0000ff));
        }

        //protected void writeInt8(int v)
        //{
        //    this.output += chr(v & 0xff);
        //}
        protected void writeInt8(int v)
        {
            this.buffer.Add((byte)(v & 0xff));
        }

        protected void writeInternal(ProtocolTreeNode node)
        {
            int len = 1;
            if (node.attributeHash != null)
            {
                len += node.attributeHash.Count() * 2;
            }
            if (node.children.Any())
            {
                len += 1;
            }
            if (node.data.Length > 0)
            {
                len += 1;
            }
            this.writeListStart(len);
            this.writeString(node.tag);
            this.writeAttributes(node.attributeHash);
            if (node.data.Length > 0)
            {
                this.writeBytes(node.data);
            }
            if (node.children != null && node.children.Any())
            {
                this.writeListStart(node.children.Count());
                foreach (var item in node.children)
                {
                    this.writeInternal(item);
                }
            }
        }
        protected void writeJid(string user, string server)
        {
            //this.output += "\xfa";
            this.buffer.Add(0xfa);
            if (user.Length > 0)
            {
                this.writeString(user);
            }
            else
            {
                this.writeToken(0);
            }
            this.writeString(server);
        }

        protected void writeListStart(int len)
        {
            if (len == 0)
            {
                //this.output += "\x00";
                this.buffer.Add(0x00);
            }
            else if (len < 256)
            {
                //this.output += "\xf8";
                this.buffer.Add(0xf8);
                this.writeInt8(len);
            }
            else
            {
                //this.output += "\xf9";
                this.buffer.Add(0xf9);
                this.writeInt16(len);
            }
        }

        protected void writeString(string tag)
        {
            if (this.tokenMap.ContainsKey(tag))
            {
                int value = this.tokenMap[tag];
                this.writeToken(value);
            }
            else
            {
                int index = tag.IndexOf('@');
                if (index != -1)
                {
                    string server = tag.Substring(index + 1);
                    string user = tag.Substring(0, index);
                    this.writeJid(user, server);
                }
                else
                {
                    //this.writeBytes(tag);
                    this.writeBytes(tag);
                }
            }
        }

        //protected void writeToken(int token)
        //{
        //    if (token < 0xf5)
        //    {
        //        this.output += chr(token);
        //    }
        //    else if (token <= 0x1f4)
        //    {
        //        this.output += "\xfe" + chr(token - 0xf5);
        //    }
        //}
        protected void writeToken(int token)
        {
            if (token < 0xf5)
            {
                this.buffer.Add((byte)token);
            }
            else if (token <= 0x1f4)
            {
                this.buffer.Add(0xfe);
                this.buffer.Add((byte)(token - 0xf5));
            }
        }

        protected void DebugPrint(string debugMsg)
        {
            if (WhatsApp.DEBUG && debugMsg.Length > 0)
            {
                Console.WriteLine(debugMsg);
            }
        }
    }
}
