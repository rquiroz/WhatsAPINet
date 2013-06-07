using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;
using WhatsAppApi.Settings;

namespace WhatsAppApi
{
    /// <summary>
    /// Main api interface
    /// </summary>
    public class WhatsApp
    {

        /// <summary>
        /// Describes the connection status with the whatsapp server
        /// </summary>
        public enum CONNECTION_STATUS
        {
            UNAUTHORIZED,
            DISCONNECTED,
            CONNECTED,
            LOGGEDIN
        }

        private ProtocolTreeNode uploadResponse;
    
        /// <summary>
        /// An instance of the AccountInfo class
        /// </summary>
        private AccountInfo accountinfo;

        /// <summary>
        /// Determines wether debug mode is on or offf
        /// </summary>
        public static bool DEBUG;

        /// <summary>
        /// The imei/mac adress
        /// </summary>
        private string imei;

        /// <summary>
        /// Holds the login status
        /// </summary>
        private CONNECTION_STATUS loginStatus;

        public CONNECTION_STATUS ConnectionStatus
        {
            get
            {
                return this.loginStatus;
            }
        }

        /// <summary>
        /// A lock for a message
        /// </summary>
        private object messageLock = new object();

        /// <summary>
        /// Que for recieved messages
        /// </summary>
        private List<ProtocolTreeNode> messageQueue;

        /// <summary>
        /// The name of the user
        /// </summary>
        private string name;

        /// <summary>
        /// The phonenumber
        /// </summary>
        private string phoneNumber;

        /// <summary>
        /// An instance of the BinaryTreeNodeReader class
        /// </summary>
        private BinTreeNodeReader reader;

        /// <summary>
        /// An instance of the BinaryTreeNodeWriter class
        /// </summary>
        private BinTreeNodeWriter writer;

        /// <summary>
        /// The timeout for the connection with the Whatsapp servers
        /// </summary>
        private int timeout = 300000;

        /// <summary>
        /// An instance of the WhatsNetwork class
        /// </summary>
        private WhatsNetwork whatsNetwork;

        /// <summary>
        /// An instance of the WhatsSendHandler class
        /// </summary>
        public WhatsSendHandler WhatsSendHandler { get; private set; }

        /// <summary>
        /// An instance of the WhatsParser class
        /// </summary>
        public WhatsParser WhatsParser { get; private set; }

        /// <summary>
        /// Holds the encoding we use, default is UTF8
        /// </summary>
        public static readonly Encoding SYSEncoding = Encoding.UTF8;

        /// <summary>
        /// Empty bytes to hold the encryption key
        /// </summary>
        private byte[] _encryptionKey;

        /// <summary>
        /// Empty bytes to hold the challenge
        /// </summary>
        private byte[] _challengeBytes;

        /// <summary>
        /// A list of exceptions for incomplete bytes
        /// </summary>
        private List<IncompleteMessageException> _incompleteBytes;

        /// <summary>
        /// Default class constructor
        /// </summary>
        /// <param name="phoneNum">The phone number</param>
        /// <param name="imei">The imei / mac</param>
        /// <param name="nick">User nickname</param>
        /// <param name="debug">Debug on or off, false by default</param>
        public WhatsApp(string phoneNum, string imei, string nick, bool debug = false)
        {
            this.messageQueue = new List<ProtocolTreeNode>();

            this.phoneNumber = phoneNum;
            this.imei = imei;
            this.name = nick;
            WhatsApp.DEBUG = debug;
            string[] dict = DecodeHelper.getDictionary();
            this.writer = new BinTreeNodeWriter(dict);
            this.reader = new BinTreeNodeReader(dict);
            this.loginStatus = CONNECTION_STATUS.DISCONNECTED;
            this.whatsNetwork = new WhatsNetwork(WhatsConstants.WhatsAppHost, WhatsConstants.WhatsPort, this.timeout);
            this.WhatsParser = new WhatsParser(this.whatsNetwork, this.writer);
            this.WhatsSendHandler = this.WhatsParser.WhatsSendHandler;

            _incompleteBytes = new List<IncompleteMessageException>();
        }

        /// <summary>
        /// Add a message to the message que
        /// </summary>
        /// <param name="node">An instance of the ProtocolTreeNode class</param>
        public void AddMessage(ProtocolTreeNode node)
        {
            lock (messageLock)
            {
                this.messageQueue.Add(node);
            }

        }

        /// <summary>
        /// Connect to the whatsapp network
        /// </summary>
        public void Connect()
        {
            this.whatsNetwork.Connect();
        }

        /// <summary>
        /// Disconnect from the whatsapp network
        /// </summary>
        public void Disconnect()
        {
            this.whatsNetwork.Disconenct();
            this.loginStatus = CONNECTION_STATUS.DISCONNECTED;
        }

        /// <summary>
        /// Encrypt the password (hash)
        /// </summary>
        /// <returns></returns>
        public byte[] encryptPassword()
        {
            return Convert.FromBase64String(this.imei);
        }

        /// <summary>
        /// Get the account information
        /// </summary>
        /// <returns>An instance of the AccountInfo class</returns>
        public AccountInfo GetAccountInfo()
        {
            return this.accountinfo;
        }

        public void GetStatus(string jid)
        {
            this.WhatsSendHandler.SendGetStatus(this.GetJID(jid));
        }

        public void PresenceSubscription(string target)
        {
            this.WhatsSendHandler.SendPresenceSubscriptionRequest(this.GetJID(target));
        }

        /// <summary>
        /// Retrieve all messages
        /// </summary>
        /// <returns>An array of instances of the ProtocolTreeNode class.</returns>
        public ProtocolTreeNode[] GetAllMessages()
        {
            ProtocolTreeNode[] tmpReturn = null;
            lock (messageLock)
            {
                tmpReturn = this.messageQueue.ToArray();
                this.messageQueue.Clear();
            }
            return tmpReturn;
        }

        /// <summary>
        /// Checks wether we have messages to retrieve
        /// </summary>
        /// <returns>true or false</returns>
        public bool HasMessages()
        {
            if (this.messageQueue == null)
                return false;
            return this.messageQueue.Count > 0;
        }

        /// <summary>
        /// Logs us in to the server
        /// </summary>
        public void Login()
        {
            //reset stuff
            this.reader.Encryptionkey = null;
            this.writer.Encryptionkey = null;
            this._challengeBytes = null;
            Encryption.encryptionIncoming = null;
            Encryption.encryptionOutgoing = null;

            string resource = string.Format(@"{0}-{1}-{2}",
                WhatsConstants.IphoneDevice,
                WhatsConstants.WhatsAppVer,
                WhatsConstants.WhatsPort);
            var data = this.writer.StartStream(WhatsConstants.WhatsAppServer, resource);
            var feat = this.addFeatures();
            var auth = this.addAuth();
            this.whatsNetwork.SendData(data);
            this.whatsNetwork.SendData(this.writer.Write(feat, false));
            this.whatsNetwork.SendData(this.writer.Write(auth, false));
            this.PollMessages();
            ProtocolTreeNode authResp = this.addAuthResponse();
            this.whatsNetwork.SendData(this.writer.Write(authResp, false));
            int cnt = 0;
            do
            {
                this.PollMessages();
                System.Threading.Thread.Sleep(50);
            } 
            while ((cnt++ < 100) && (this.loginStatus == CONNECTION_STATUS.DISCONNECTED));
        }

        /// <summary>
        /// Send a message to a person
        /// </summary>
        /// <param name="to">The phone number to send</param>
        /// <param name="txt">The text that needs to be send</param>
        public void Message(string to, string txt)
        {
            var tmpMessage = new FMessage(this.GetJID(to), true) { key = { id = TicketManager.GenerateId() }, data = txt };
            this.WhatsParser.WhatsSendHandler.SendMessage(tmpMessage);
        }

        /// <summary>
        /// Convert the input string to a JID if necessary
        /// </summary>
        /// <param name="target">Phonenumber or JID</param>
        public string GetJID(string target)
        {
            if (!target.Contains('@'))
            {
                //check if group message
                if (target.Contains('-'))
                {
                    //to group
                    target += "@g.us";
                }
                else
                {
                    //to normal user
                    target += "@s.whatsapp.net";
                }
            }
            return target;
        }

        /// <summary>
        /// Send an image to a person
        /// </summary>
        /// <param name="msgid">The id of the message</param>
        /// <param name="to">the reciepient</param>
        /// <param name="url">The url to the image</param>
        /// <param name="file">Filename</param>
        /// <param name="size">The size of the image in string format</param>
        /// <param name="icon">Icon</param>
        public void MessageImage(string to, string filepath)
        {
            FileInfo finfo = new FileInfo(filepath);
            string type = string.Empty;
            switch (finfo.Extension)
            {
                case ".png":
                    type = "image/png";
                    break;
                case ".gif":
                    type = "image/gif";
                    break;
                default:
                    type = "image/jpeg";
                    break;
            }
            
            //create hash
            string filehash = string.Empty;
            using(FileStream fs = File.OpenRead(filepath))
            {
                using(BufferedStream bs = new BufferedStream(fs))
                {
                    using(HashAlgorithm sha = HashAlgorithm.Create("sha256"))
                    {
                        byte[] raw = sha.ComputeHash(bs);
                        filehash = Convert.ToBase64String(raw);
                    }
                }
            }

            //request upload
            string url = this.UploadFile(filehash, "image", finfo.Length, filepath, to, type);
        }

        private string UploadFile(string b64hash, string type, long size, string path, string to, string contenttype)
        {
            ProtocolTreeNode media = new ProtocolTreeNode("media", new KeyValue[] {
                new KeyValue("xmlns", "w:m"),
                new KeyValue("hash", b64hash),
                new KeyValue("type", type),
                new KeyValue("size", size.ToString())
            });
            string id = TicketManager.GenerateId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new KeyValue[] {
                new KeyValue("id", id),
                new KeyValue("to", WhatsConstants.WhatsAppServer),
                new KeyValue("type", "set")
            }, media);
            this.uploadResponse = null;
            this.WhatsSendHandler.SendNode(node);
            int i = 0;
            while (this.uploadResponse == null && i < 5)
            {
                i++;
                this.PollMessages();
            }
            try
            {
                string uploadUrl = this.uploadResponse.GetChild("media").GetAttribute("url");
                this.uploadResponse = null;

                Uri uri = new Uri(uploadUrl);

                string hashname = string.Empty;
                byte[] buff = MD5.Create().ComputeHash(System.Text.Encoding.Default.GetBytes(path));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in buff)
                {
                    sb.Append(b.ToString("X2"));
                }
                hashname = String.Format("{0}.{1}", sb.ToString(), path.Split('.').Last());

                string boundary = "zzXXzzYYzzXXzzQQ";

                sb = new StringBuilder();

                sb.AppendFormat("--{0}\r\n", boundary);
                sb.Append("Content-Disposition: form-data; name=\"to\"\r\n\r\n");
                sb.AppendFormat("{0}\r\n", to);
                sb.AppendFormat("--{0}\r\n", boundary);
                sb.Append("Content-Disposition: form-data; name=\"from\"\r\n\r\n");
                sb.AppendFormat("{0}\r\n", this.phoneNumber);
                sb.AppendFormat("{0}\r\n", boundary);
                sb.AppendFormat("Content-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\n", hashname);
                sb.AppendFormat("Content-Type: {0}\r\n\r\n", contenttype);
                string header = sb.ToString();

                sb = new StringBuilder();
                sb.AppendFormat("\r\n--{0}--\r\n", boundary);
                string footer = sb.ToString();

                long clength = size + header.Length + footer.Length;

                sb = new StringBuilder();
                sb.AppendFormat("POST {0}\r\n", uploadUrl);
                sb.AppendFormat("Content-Type: multipart/form-data; boundary={0}\r\n", boundary);
                sb.AppendFormat("Host: {0}\r\n", uri.Host);
                sb.AppendFormat("User-Agent: {0}\r\n", WhatsConstants.UserAgent);
                sb.AppendFormat("Content-Length: {0}\r\n\r\n", clength);
                string post = sb.ToString();

                TcpClient tc = new TcpClient(uri.Host, 443);
                SslStream ssl = new SslStream(tc.GetStream());
                try
                {
                    ssl.AuthenticateAsClient(uri.Host);
                }
                catch (Exception e)
                {
                    throw e;
                }
                byte[] msgdata = Encoding.ASCII.GetBytes(post);
                ssl.Write(msgdata);
                msgdata = Encoding.ASCII.GetBytes(header);
                ssl.Write(msgdata);
                buff = File.ReadAllBytes(path);
                ssl.Write(buff);
                msgdata = Encoding.ASCII.GetBytes(footer);
                ssl.Write(msgdata);

                //moment of truth...
                ssl.Read(buff, 0, buff.Length);
                string json = System.Text.Encoding.ASCII.GetString(buff);
                return json;
            }
            catch (Exception e)
            { }
            return null;
        }

        /// <summary>
        /// Retrieve messages from the server
        /// </summary>
        public void PollMessages()
        {
            this.processInboundData(this.whatsNetwork.ReadData());
        }

        /// <summary>
        /// Send a pong to the whatsapp server
        /// </summary>
        /// <param name="msgid">Message id</param>
        public void Pong(string msgid)
        {
            this.WhatsParser.WhatsSendHandler.SendPong(msgid);
        }

        /// <summary>
        /// Request last seen
        /// </summary>
        /// <param name="jid">Jabber id</param>
        public void RequestLastSeen(string jid)
        {
            this.WhatsParser.WhatsSendHandler.SendQueryLastOnline(this.GetJID(jid));
        }

        /// <summary>
        /// Send nickname
        /// </summary>
        /// <param name="nickname">The nickname</param>
        public void sendNickname(string nickname)
        {
            this.WhatsParser.WhatsSendHandler.SendAvailableForChat(nickname);
        }

        /// <summary>
        /// Add the authenication nodes
        /// </summary>
        /// <returns>An instance of the ProtocolTreeNode class</returns>
        protected ProtocolTreeNode addAuth()
        {
            var node = new ProtocolTreeNode("auth",
                new KeyValue[] { new KeyValue("xmlns", @"urn:ietf:params:xml:ns:xmpp-sasl"),
                new KeyValue("mechanism", "WAUTH-1"),
                new KeyValue("user", this.phoneNumber) });
            return node;
        }

        /// <summary>
        /// Add the auth response to protocoltreenode
        /// </summary>
        /// <returns>An instance of the ProtocolTreeNode</returns>
        protected ProtocolTreeNode addAuthResponse()
        {
            while (this._challengeBytes == null)
            {
                this.PollMessages();
                System.Threading.Thread.Sleep(500);
            }

            Rfc2898DeriveBytes r = new Rfc2898DeriveBytes(this.encryptPassword(), _challengeBytes, 16);
            this._encryptionKey = r.GetBytes(20);
            this.reader.Encryptionkey = _encryptionKey;
            this.writer.Encryptionkey = _encryptionKey;

            List<byte> b = new List<byte>();
            b.AddRange(WhatsApp.SYSEncoding.GetBytes(this.phoneNumber));
            b.AddRange(this._challengeBytes);
            b.AddRange(WhatsApp.SYSEncoding.GetBytes(Func.GetNowUnixTimestamp().ToString()));

            byte[] data = b.ToArray();

            byte[] response = Encryption.WhatsappEncrypt(_encryptionKey, data, false);
            var node = new ProtocolTreeNode("response",
                new KeyValue[] { new KeyValue("xmlns", "urn:ietf:params:xml:ns:xmpp-sasl") },
                response);

            return node;
        }

        /// <summary>
        /// Add stream features
        /// </summary>
        /// <returns></returns>
        protected ProtocolTreeNode addFeatures()
        {
            var child = new ProtocolTreeNode("receipt_acks", null);
            var childList = new List<ProtocolTreeNode>();
            childList.Add(child);
            var parent = new ProtocolTreeNode("stream:features", null, childList, null);
            return parent;
        }

        /// <summary>
        /// Print a message to the debug console
        /// </summary>
        /// <param name="debugMsg">The message</param>
        protected void DebugPrint(string debugMsg)
        {
            if (WhatsApp.DEBUG && debugMsg.Length > 0)
            {
                Console.WriteLine(debugMsg);
            }
        }

        /// <summary>
        /// Process the challenge
        /// </summary>
        /// <param name="node">The node that contains the challenge</param>
        protected void processChallenge(ProtocolTreeNode node)
        {
            _challengeBytes = node.data;
        }
        
        /// <summary>
        /// Process inbound data
        /// </summary>
        /// <param name="data">Data to process</param>
        protected void processInboundData(byte[] data)
        {
            try
            {
                List<byte> foo = new List<byte>();
                foreach (IncompleteMessageException e in this._incompleteBytes)
                {
                    foo.AddRange(e.getInput());
                }
                this._incompleteBytes = new List<IncompleteMessageException>();
                foo.AddRange(data);
                ProtocolTreeNode node = this.reader.nextTree(foo.ToArray());
                while (node != null)
                {
                    //this.WhatsParser.ParseProtocolNode(node);
                    if (node.tag == "iq"
                    && node.GetAttribute("type") == "error")
                    {
                        this.AddMessage(node);
                    }
                    if (ProtocolTreeNode.TagEquals(node, "challenge"))
                    {
                        this.processChallenge(node);
                    } 
                    else if (ProtocolTreeNode.TagEquals(node,"success"))
                    {
                        this.loginStatus = CONNECTION_STATUS.LOGGEDIN;
                        this.accountinfo = new AccountInfo(node.GetAttribute("status"),
                                                           node.GetAttribute("kind"),
                                                           node.GetAttribute("creation"),
                                                           node.GetAttribute("expiration"));
                    }
                    else if (ProtocolTreeNode.TagEquals(node,"failure"))
                    {
                        this.loginStatus = CONNECTION_STATUS.UNAUTHORIZED;
                    }
                    if (ProtocolTreeNode.TagEquals(node,"message"))
                    {
                        this.AddMessage(node);
                        if (node.GetChild("received") == null)
                        {
                            this.sendMessageReceived(node);
                        }
                    }
                    if (ProtocolTreeNode.TagEquals(node,"stream:error"))
                    {
                        Console.Write(node.NodeString());
                    }
                    if (ProtocolTreeNode.TagEquals(node, "iq")
                        && node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                        && ProtocolTreeNode.TagEquals(node.children.First(), "query")
                        )
                    {
                        //last seen
                        this.AddMessage(node);
                    }
                    if (ProtocolTreeNode.TagEquals(node, "iq")
                        && node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                        && ProtocolTreeNode.TagEquals(node.children.First(), "media")
                        )
                    {
                        //media upload
                        this.uploadResponse = node;
                    }
                    if (ProtocolTreeNode.TagEquals(node, "iq")
                        && node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                        && ProtocolTreeNode.TagEquals(node.children.First(), "picture")
                        )
                    {
                        //profile picture
                        this.AddMessage(node);
                    }
                    if (ProtocolTreeNode.TagEquals(node,"iq")
                        && node.GetAttribute("type").Equals("get", StringComparison.OrdinalIgnoreCase)
                        && ProtocolTreeNode.TagEquals(node.children.First(), "ping"))
                    {
                        this.Pong(node.GetAttribute("id"));
                    }
                    if (ProtocolTreeNode.TagEquals(node ,"stream:error"))
                    {
                        var textNode = node.GetChild("text");
                        if (textNode != null)
                        {
                            string content = WhatsApp.SYSEncoding.GetString(textNode.GetData());
                            Console.WriteLine("Error : " + content);
                            if (content.Equals("Replaced by new connection", StringComparison.OrdinalIgnoreCase))
                            {
                                this.Disconnect();
                                this.Connect();
                                this.Login();
                            }
                        }
                    }
                    if (ProtocolTreeNode.TagEquals(node, "presence"))
                    {
                        //presence node
                        this.AddMessage(node);
                    }
                    node = this.reader.nextTree();
                }
            }
            catch (IncompleteMessageException ex)
            {
                this._incompleteBytes.Add(ex);
            }
        }

        /// <summary>
        /// Tell the server we recieved the message
        /// </summary>
        /// <param name="msg">The ProtocolTreeNode that contains the message</param>
        protected void sendMessageReceived(ProtocolTreeNode msg)
        {
            ProtocolTreeNode requestNode = msg.GetChild("request");
            if (requestNode == null ||
                !requestNode.GetAttribute("xmlns").Equals("urn:xmpp:receipts", StringComparison.OrdinalIgnoreCase))
                return;

            FMessage tmpMessage = new FMessage(new FMessage.Key(msg.GetAttribute("from"), true, msg.GetAttribute("id")));
            this.WhatsParser.WhatsSendHandler.SendMessageReceived(tmpMessage);
        }

        /// <summary>
        /// MD5 hashes the password
        /// </summary>
        /// <param name="pass">String the needs to be hashed</param>
        /// <returns>A md5 hash</returns>
        private string md5(string pass)
        {
            MD5 md5 = MD5.Create();
            byte[] dataMd5 = md5.ComputeHash(WhatsApp.SYSEncoding.GetBytes(pass));
            var sb = new StringBuilder();
            for (int i = 0; i < dataMd5.Length; i++)
                sb.AppendFormat("{0:x2}", dataMd5[i]);
            return sb.ToString();
        }

        /// <summary>
        /// Prints debug
        /// </summary>
        /// <param name="p">message</param>
        private void PrintInfo(string p)
        {
            this.DebugPrint(p);
        }
    }
}
