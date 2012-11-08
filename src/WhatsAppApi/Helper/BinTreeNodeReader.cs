using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    internal class BinTreeNodeReader
    {
        private string[] dictionary;
        //private string input;

        //change to protocol 1.2
        public byte[] Encryptionkey { get; set; }
        private List<byte> buffer;

        public BinTreeNodeReader(string[] dict)
        {
            this.dictionary = dict;
            this.Encryptionkey = null;
        }

        //public ProtocolTreeNode nextTree(string pInput = null)
        //{
        //    if (pInput != null)
        //    {
        //        this.input = pInput;
        //    }

        //    //int stanzaSize = this.peekInt16();
        //    //Change to protocol 1.2
        //    int stanzaSize = this.peekInt24();
        //    int flags = (stanzaSize >> 20);
        //    int size = ((stanzaSize & 0xF0000) >> 16) | ((stanzaSize & 0xFF00) >> 8) | (stanzaSize & 0xFF);

        //    bool isEncrypted = ((flags & 8) != 0); // 8 = (1 << 4) // Read node and decrypt

        //    if (stanzaSize > this.input.Length)
        //    {
        //        //Es sind noch nicht alle Daten eingelesen, daher abbrechen und warten bis alles da ist
        //        var exception = new IncompleteMessageException("Incomplete message");
        //        exception.setInput(this.input);
        //        throw exception;
        //    }

        //    this.readInt24();
        //    if (stanzaSize > 0)
        //    {
        //        if (isEncrypted && Encryptionkey != null)
        //        {
        //            RC4 encryption = new RC4(this.Encryptionkey, 256);
        //            byte[] dataB = this.sysEncoding.GetBytes(this.input);
        //            byte[] enData = new byte[dataB.Length - 3];
        //            Buffer.BlockCopy(dataB, 3, enData, 0, dataB.Length - 3);
        //            //encryption.Cipher(enData, 0, dataB.Length - 3);
        //            enData = encryption.Encrypt(enData);
        //            Buffer.BlockCopy(enData, 0, dataB, 3, enData.Length);
        //        }
        //        return this.nextTreeInternal();
        //    }
        //    return null;
        //}
        public ProtocolTreeNode nextTree(byte[] pInput = null)
        {
            if (pInput != null)
            {
                if (pInput.Length == 0)
                    return null;
                this.buffer = new List<byte>();
                this.buffer.AddRange(pInput);
            }

            // Ported from the allegedly working PHP version ~lrg
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

            if (isEncrypted && Encryptionkey != null)
            {
                decode(ref this.buffer, size);
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

        protected void decode(ref List<byte> buffer, int stanzaSize)
        {
            int size = stanzaSize;
            byte[] data = new byte[size];
            byte[] dataReal = null;
            Buffer.BlockCopy(buffer.ToArray(), 0, data, 0, size);

            byte[] packet = new byte[size - 4];

            byte[] hashServerByte = new byte[4];
            Buffer.BlockCopy(data, 0, hashServerByte, 0, 4);
            Buffer.BlockCopy(data, 4, packet, 0, size - 4);

            System.Security.Cryptography.HMACSHA1 h = new System.Security.Cryptography.HMACSHA1(this.Encryptionkey);
            byte[] hashByte = new byte[4];
            Buffer.BlockCopy(h.ComputeHash(packet, 0, packet.Length), 0, hashByte, 0, 4);

            // 20121107 not sure why the packet is indicated an ecrypted but the hmcash1 is incorrect
            if (hashServerByte.SequenceEqual(hashByte))
            {
                this.buffer.RemoveRange(0, 4);
                dataReal = Encryption.WhatsappDecrypt(this.Encryptionkey, packet);

                for (int i = 0; i < size - 4; i++)
                {
                    this.buffer[i] = dataReal[i];
                }
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
                //throw new Exception("BinTreeNodeReader->getToken: Invalid token $token");
            }
            return ret;
        }

        //protected string readString(int token)
        //{
        //    string ret = "";
        //    if (token == -1)
        //    {
        //        throw new Exception("BinTreeNodeReader->readString: Invalid token $token");
        //    }
        //    if ((token > 4) && (token < 0xf5))
        //    {
        //        ret = this.getToken(token);
        //    }
        //    else if (token == 0)
        //    {
        //        ret = "";
        //    }
        //    else if (token == 0xfc)
        //    {
        //        int size = this.readInt8();
        //        ret = WhatsApp.SYSEncoding.GetString(this.fillArray(size));
        //    }
        //    else if (token == 0xfd)
        //    {
        //        int size = this.readInt24();
        //        ret = WhatsApp.SYSEncoding.GetString(this.fillArray(size));
        //    }
        //    else if (token == 0xfe)
        //    {
        //        int tmpToken = this.readInt8();
        //        ret = this.getToken(tmpToken + 0xf5);
        //    }
        //    else if (token == 0xfa)
        //    {
        //        string user = this.readString(this.readInt8());
        //        string server = this.readString(this.readInt8());
        //        if ((user.Length > 0) && (server.Length > 0))
        //        {
        //            ret = user + "@" + server;
        //        }
        //        else if (server.Length > 0)
        //        {
        //            ret = server;
        //        }
        //    }
        //    return ret;
        //}

        protected byte[] readBytes(int token)
        {
            byte[] ret = new byte[0];
            if (token == -1)
            {
                throw new Exception("BinTreeNodeReader->readString: Invalid token $token");
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
            int token = this.readInt8();
            int size = this.readListSize(token);
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
            //string tag = this.readString(token);
            string tag = WhatsApp.SYSEncoding.GetString(this.readBytes(token2));
            var tmpAttributes = this.readAttributes(size);
            //if (size == 0 || string.IsNullOrWhiteSpace(tag))
            //{
            //    return null;
            //}
            if ((size % 2) == 1)
            {
                return new ProtocolTreeNode(tag, tmpAttributes);
            }
            int token3 = this.readInt8();
            if (this.isListTag(token3))
            {
                //return new ProtocolTreeNode(tag, tmpAttributes, this.readList(token), "");
                return new ProtocolTreeNode(tag, tmpAttributes, this.readList(token3));
            }
            //return new ProtocolTreeNode(tag, tmpAttributes, WhatsApp.SYSEncoding.GetBytes(this.readString(token)));
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
                throw new Exception("BinTreeNodeReader->readListSize: Invalid token $token");
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
            //if (this.input.Length >= 3)
            //{
            //    ret = (int)this.input[0] << 16;
            //    ret |= (int)this.input[1] << 8;
            //    ret |= (int)this.input[2] << 0;
            //}
            if (this.buffer.Count >= 3 + offset)
            {
                //    ret = this.buffer[0] << 16;
                //    ret |= this.buffer[1] << 8;
                //    ret |= this.buffer[2] << 0;
                ret = (this.buffer[0 + offset] << 16) + (this.buffer[1 + offset] << 8) + this.buffer[2 + offset];
            }
            return ret;
        }
        
        protected int readInt24()
        {
            int ret = 0;
            //if (this.input.Length >= 3)
            //{
            //    ret = (int)this.input[0] << 16;
            //    ret |= (int)this.input[1] << 8;
            //    ret |= (int)this.input[2] << 0;
            //    this.input = this.input.Remove(0, 3);
            //}
            if (this.buffer.Count >= 3)
            {
                ret = this.buffer[0] << 16;
                ret |=this.buffer[1] << 8;
                ret |=this.buffer[2] << 0;
                this.buffer.RemoveRange(0, 3);
            }
            return ret;
        }

        //protected int peekInt16()
        //{
        //    int ret = 0;
        //    if (this.input.Length >= 2)
        //    {
        //        ret = (int)this.input[0] << 8;
        //        ret |= (int)this.input[1] << 0;
        //    }
        //    return ret;
        //}
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

        //protected int readInt16()
        //{
        //    int ret = 0;
        //    if (this.input.Length >= 2)
        //    {
        //        ret = (int)this.input[0] << 8;
        //        ret |= (int)this.input[1] << 0;
        //        this.input = this.input.Remove(0, 2);
        //    }
        //    return ret;
        //}
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

        //protected int readInt8()
        //{
        //    int ret = 0;
        //    if (this.input.Length >= 1)
        //    {
        //        ret = (int)this.input[0];
        //        this.input = this.input.Remove(0, 1);
        //    }
        //    return ret;
        //}
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

        //protected string fillArray(int len)
        //{
        //    string ret = "";
        //    if (this.input.Length >= len)
        //    {
        //        ret = this.input.Substring(0, len);
        //        this.input = this.input.Remove(0, len);
        //    }
        //    return ret;
        //}
        protected byte[] fillArray(int len)
        {
            byte[] ret = new byte[len];
            if (this.buffer.Count >= len)
            {
                //this.buffer.CopyTo(0, ret, 0, len);
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
