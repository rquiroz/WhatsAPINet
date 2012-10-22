using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    internal class BinTreeNodeReader
    {
        private string[] dictionary;
        private string input;

        public BinTreeNodeReader(string[] dict)
        {
            this.dictionary = dict;
        }

        public ProtocolTreeNode nextTree(string pInput = null)
        {
            if (pInput != null)
            {
                this.input = pInput;
            }

            int stanzaSize = this.peekInt16();

            if (stanzaSize > this.input.Length)
            {
                //Es sind noch nicht alle Daten eingelesen, daher abbrechen und warten bis alles da ist
                var exception = new IncompleteMessageException("Incomplete message");
                exception.setInput(this.input);
                throw exception;
            }

            this.readInt16();
            if (stanzaSize > 0)
            {
                return this.nextTreeInternal();
            }
            return null;
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
                throw new Exception("BinTreeNodeReader->getToken: Invalid token $token");
            }
            return ret;
        }

        protected string readString(int token)
        {
            string ret = "";
            if (token == -1)
            {
                throw new Exception("BinTreeNodeReader->readString: Invalid token $token");
            }
            if ((token > 4) && (token < 0xf5))
            {
                ret = this.getToken(token);
            }
            else if (token == 0)
            {
                ret = "";
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
                ret = this.getToken(tmpToken + 0xf5);
            }
            else if (token == 0xfa)
            {
                string user = this.readString(this.readInt8());
                string server = this.readString(this.readInt8());
                if ((user.Length > 0) && (server.Length > 0))
                {
                    ret = user + "@" + server;
                }
                else if (server.Length > 0)
                {
                    ret = server;
                }
            }
            return ret;
        }

        protected IEnumerable<KeyValue> readAttributes(int size)
        {
            var attributes = new List<KeyValue>();
            int attribCount = (size - 2 + size%2)/2;
            for (int i = 0; i < attribCount; i++)
            {
                string key = this.readString(this.readInt8());
                string value = this.readString(this.readInt8());
                attributes.Add(new KeyValue(key, value));
            }
            return attributes;
        }

        protected ProtocolTreeNode nextTreeInternal()
        {
            int token = this.readInt8();
            int size = this.readListSize(token);
            token = this.readInt8();

            if (token == 1)
            {
                var attributes = this.readAttributes(size);
                return new ProtocolTreeNode("start", attributes);
            }
            else if (token == 2)
            {
                return null;
            }
            string tag = this.readString(token);
            var tmpAttributes = this.readAttributes(size);

            if ((size%2) == 1)
            {
                return new ProtocolTreeNode(tag, tmpAttributes);
            }
            token = this.readInt8();
            if (this.isListTag(token))
            {
                return new ProtocolTreeNode(tag, tmpAttributes, this.readList(token), "");
            }
            return new ProtocolTreeNode(tag, tmpAttributes, this.readString(token));
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
            if (token == 0xf8)
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

        protected int readInt24()
        {
            int ret = 0;
            if (this.input.Length >= 3)
            {
                ret = (int) this.input[0] << 16;
                ret |= (int) this.input[1] << 8;
                ret |= (int) this.input[2] << 0;
                this.input = this.input.Remove(0, 3);
            }
            return ret;
        }

        protected int peekInt16()
        {
            int ret = 0;
            if (this.input.Length >= 2)
            {
                ret = (int) this.input[0] << 8;
                ret |= (int) this.input[1] << 0;
            }
            return ret;
        }

        protected int readInt16()
        {
            int ret = 0;
            if (this.input.Length >= 2)
            {
                ret = (int)this.input[0] << 8;
                ret |= (int)this.input[1] << 0;
                this.input = this.input.Remove(0, 2);
            }
            return ret;
        }

        protected int readInt8()
        {
            int ret = 0;
            if (this.input.Length >= 1)
            {
                ret = (int) this.input[0];
                this.input = this.input.Remove(0, 1);
            }
            return ret;
        }

        protected string fillArray(int len)
        {
            string ret = "";
            if (this.input.Length >= len)
            {
                ret = this.input.Substring(0, len);
                this.input = this.input.Remove(0, len);
            }
            return ret;
        }
    }

}
