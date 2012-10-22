using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    internal class BinTreeNodeWriter
    {
        private string output;
        private Dictionary<string, int> tokenMap;

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
        }

        public string StartStream(string domain, string resource)
        {
            var attributes = new List<KeyValue>();
            this.output = "WA";
            this.output += "\x01" + "\x01" +"\x00" + "\x19";

            attributes.Add(new KeyValue("to", domain));
            attributes.Add(new KeyValue("resource", resource));
            this.writeListStart(attributes.Count*2 + 1);

            this.output += "\x01";
            this.writeAttributes(attributes.ToArray());
            string ret = this.output;
            this.output = "";
            return ret;
        }

        public string Write(ProtocolTreeNode node)
        {
            if (node == null)
            {
                this.output += "\x00";
            }
            else
            {
                this.writeInternal(node);
            }
            return this.flushBuffer();
        }

        protected string flushBuffer()
        {
            int size = this.output.Length;
            this.output = this.GetInt16(size) + this.output;
            string ret = this.output;
            this.output = "";
            return ret;
        }

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

        private string GetInt16(int len)
        {
            string ret = chr((len & 0xff00) >> 8);
            ret += chr((len & 0x00ff) >> 0);
            return ret;
        }

        protected void writeBytes(string bytes)
        {
            int len = bytes.Length;
            if (len >= 0x100)
            {
                this.output += "\xfd";
                this.writeInt24(len);
            }
            else
            {
                this.output += "\xfc";
                this.writeInt8(len);
            }
            this.output += bytes;
        }

        protected void writeInt16(int v)
        {
            //string ret = "";
            this.output += chr((v & 0xff00) >> 8);
            this.output += chr((v & 0x00ff) >> 0);
            //this.output = ret + this.output;
        }

        protected void writeInt24(int v)
        {
            this.output += chr((v & 0xff0000) >> 16);
            this.output += chr((v & 0x00ff00) >> 8);
            this.output += chr((v & 0x0000ff) >> 0);
        }

        protected void writeInt8(int v)
        {
            this.output += chr(v & 0xff);
        }

        protected void writeInternal(ProtocolTreeNode node)
        {
            int len = 1;
            if (node.attributeHash != null)
            {
                len += node.attributeHash.Count()*2;
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
            this.output += "\xfa";
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
                this.output += "\x00";
            }
            else if (len < 256)
            {
                this.output += "\xf8";
                this.writeInt8(len);
            }
            else
            {
                this.output += "\xf9";
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
                    this.writeBytes(tag);
                }
            }
        }

        protected void writeToken(int token)
        {
            if (token < 0xf5)
            {
                this.output += chr(token);
            }
            else if (token <= 0x1f4)
            {
                this.output += "\xfe" + chr(token - 0xf5);
            }
        }

        /// <summary>
        /// Check if chr ist correct
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string chr(int value)
        {
            char tmpRealValue = (char)(value);
            return Convert.ToString(tmpRealValue);
        }
    }
}
