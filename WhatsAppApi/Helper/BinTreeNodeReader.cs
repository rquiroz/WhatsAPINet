using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    public class BinTreeNodeReader
    {
        private string[] dictionary;
        public byte[] Encryptionkey { get; set; }
        private List<byte> buffer;

        public BinTreeNodeReader(string[] dict)
        {
            this.dictionary = dict;
            this.Encryptionkey = null;
        }

        public ProtocolTreeNode nextTree(byte[] pInput = null, bool useDecrypt = true)
        {
            if (pInput != null)
            {
                if (pInput.Length == 0)
                    return null;
                this.buffer = new List<byte>();
                this.buffer.AddRange(pInput);
            }

            int stanzaFlag = (this.peekInt8() & 0xF0) >> 4;
            int stanzaSize = this.peekInt16(1);

            int flags = stanzaFlag;
            int size = stanzaSize;

            if (stanzaSize > this.buffer.Count)
            {
                var exception = new IncompleteMessageException("Incomplete message");
                exception.setInput(this.buffer.ToArray());
                throw exception;
            }

            this.readInt24();

            bool isEncrypted = (stanzaFlag & 8) != 0;

            if (isEncrypted)
            {
                if (Encryptionkey != null)
                {
                    decode(size, useDecrypt);
                }
                else
                {
                    throw new Exception("Received encrypted message, encryption key not set");
                }
            }

            if(stanzaSize > 0)
            {
                ProtocolTreeNode node = this.nextTreeInternal();
                if (node != null)
                    this.DebugPrint(node.NodeString("RECVD: "));
                return node;
            }
            return null;
        }

        protected void decode(int stanzaSize, bool useDecrypt)
        {
            int size = stanzaSize;
            byte[] data = new byte[size];
            byte[] dataReal = null;
            Buffer.BlockCopy(this.buffer.ToArray(), 0, data, 0, size);

            byte[] packet = new byte[size - 4];

            byte[] hashServerByte = new byte[4];
            Buffer.BlockCopy(data, 0, hashServerByte, 0, 4);
            Buffer.BlockCopy(data, 4, packet, 0, size - 4);

            System.Security.Cryptography.HMACSHA1 h = new System.Security.Cryptography.HMACSHA1(this.Encryptionkey);
            byte[] hashByte = new byte[4];
            Buffer.BlockCopy(h.ComputeHash(packet, 0, packet.Length), 0, hashByte, 0, 4);

            if (hashServerByte.SequenceEqual(hashByte))
            {
                this.buffer.RemoveRange(0, 4);
                if (useDecrypt)
                {
                    dataReal = Encryption.WhatsappDecrypt(this.Encryptionkey, packet);
                }
                else
                {
                    dataReal = Encryption.WhatsappEncrypt(this.Encryptionkey, packet, true);
                    //dataReal = new byte[foo.Length - 4];
                    //Buffer.BlockCopy(foo, 0, dataReal, 0, dataReal.Length);
                }

                for (int i = 0; i < size - 4; i++)
                {
                    this.buffer[i] = dataReal[i];
                }
            }
            else
            {
                throw new Exception("Hash doesnt match");
            }
        }

        protected string getToken(int token)
        {
            string ret = "";
            if ((token >= 0) && (token < this.dictionary.Length))
            {
                ret = this.dictionary[token];
            }
            else
            {
                throw new Exception("BinTreeNodeReader->getToken: Invalid token " + token);
            }
            return ret;
        }

        protected byte[] readBytes(int token)
        {
            byte[] ret = new byte[0];
            if (token == -1)
            {
                throw new Exception("BinTreeNodeReader->readString: Invalid token " + token);
            }
            if ((token > 4) && (token < 0xf5))
            {
                ret = WhatsApp.SYSEncoding.GetBytes(this.getToken(token));
            }
            else if (token == 0)
            {
                ret = new byte[0];
            }
            else if (token == 0xfc)
            {
                int size = this.readInt8();
                ret = this.fillArray(size);
            }
            else if (token == 0xfd)
            {
                int size = this.readInt24();
                ret = this.fillArray(size);
            }
            else if (token == 0xfe)
            {
                int tmpToken = this.readInt8();
                ret = WhatsApp.SYSEncoding.GetBytes(this.getToken(tmpToken + 0xf5));
            }
            else if (token == 0xfa)
            {
                string user = WhatsApp.SYSEncoding.GetString(this.readBytes(this.readInt8()));
                string server = WhatsApp.SYSEncoding.GetString(this.readBytes(this.readInt8()));
                if ((user.Length > 0) && (server.Length > 0))
                {
                    ret = WhatsApp.SYSEncoding.GetBytes(user + "@" + server);
                }
                else if (server.Length > 0)
                {
                    ret = WhatsApp.SYSEncoding.GetBytes(server);
                }
            }
            return ret;
        }

        protected IEnumerable<KeyValue> readAttributes(int size)
        {
            var attributes = new List<KeyValue>();
            int attribCount = (size - 2 + size % 2) / 2;
            for (int i = 0; i < attribCount; i++)
            {
                byte[] keyB = this.readBytes(this.readInt8());
                byte[] valueB = this.readBytes(this.readInt8());
                string key = WhatsApp.SYSEncoding.GetString(keyB);
                string value = WhatsApp.SYSEncoding.GetString(valueB);
                attributes.Add(new KeyValue(key, value));
            }
            return attributes;
        }

        protected ProtocolTreeNode nextTreeInternal()
        {
            int token1 = this.readInt8();
            int size = this.readListSize(token1);
            int token2 = this.readInt8();
            if (token2 == 1)
            {
                var attributes = this.readAttributes(size);
                return new ProtocolTreeNode("start", attributes);
            }
            if (token2 == 2)
            {
                return null;
            }
            string tag = WhatsApp.SYSEncoding.GetString(this.readBytes(token2));
            var tmpAttributes = this.readAttributes(size);

            if ((size % 2) == 1)
            {
                return new ProtocolTreeNode(tag, tmpAttributes);
            }
            int token3 = this.readInt8();
            if (this.isListTag(token3))
            {
                return new ProtocolTreeNode(tag, tmpAttributes, this.readList(token3));
            }

            return new ProtocolTreeNode(tag, tmpAttributes, null, this.readBytes(token3));
        }

        protected bool isListTag(int token)
        {
            return ((token == 248) || (token == 0) || (token == 249));
        }

        protected List<ProtocolTreeNode> readList(int token)
        {
            int size = this.readListSize(token);
            var ret = new List<ProtocolTreeNode>();
            for (int i = 0; i < size; i++)
            {
                ret.Add(this.nextTreeInternal());
            }
            return ret;
        }

        protected int readListSize(int token)
        {
            int size = 0;
            if (token == 0)
            {
                size = 0;
            }
            else if (token == 0xf8)
            {
                size = this.readInt8();
            }
            else if (token == 0xf9)
            {
                size = this.readInt16();
            }
            else
            {
                throw new Exception("BinTreeNodeReader->readListSize: Invalid token " + token);
            }
            return size;
        }

        protected int peekInt8(int offset = 0)
        {
            int ret = 0;

            if (this.buffer.Count >= offset + 1)
                ret = this.buffer[offset];

            return ret;
        }

        protected int peekInt24(int offset = 0)
        {
            int ret = 0;
            if (this.buffer.Count >= 3 + offset)
            {
                ret = (this.buffer[0 + offset] << 16) + (this.buffer[1 + offset] << 8) + this.buffer[2 + offset];
            }
            return ret;
        }
        
        protected int readInt24()
        {
            int ret = 0;
            if (this.buffer.Count >= 3)
            {
                ret = this.buffer[0] << 16;
                ret |=this.buffer[1] << 8;
                ret |=this.buffer[2] << 0;
                this.buffer.RemoveRange(0, 3);
            }
            return ret;
        }

        protected int peekInt16(int offset = 0)
        {
            int ret = 0;
            if (this.buffer.Count >= offset + 2)
            {
                ret = (int)this.buffer[0+offset] << 8;
                ret |= (int)this.buffer[1+offset] << 0;
            }
            return ret;
        }

        protected int readInt16()
        {
            int ret = 0;
            if (this.buffer.Count >= 2)
            {
                ret = (int)this.buffer[0] << 8;
                ret |= (int)this.buffer[1] << 0;
                this.buffer.RemoveRange(0, 2);
            }
            return ret;
        }

        protected int readInt8()
        {
            int ret = 0;
            if (this.buffer.Count >= 1)
            {
                ret = (int)this.buffer[0];
                this.buffer.RemoveAt(0);
            }
            return ret;
        }

        protected byte[] fillArray(int len)
        {
            byte[] ret = new byte[len];
            if (this.buffer.Count >= len)
            {
                Buffer.BlockCopy(this.buffer.ToArray(), 0, ret, 0, len);
                this.buffer.RemoveRange(0, len);
            }
            else
            {
                throw new Exception();
            }
            return ret;
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
