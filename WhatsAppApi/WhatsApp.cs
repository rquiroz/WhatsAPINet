using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
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

        public void ClearIncomplete()
        {
            this._incompleteBytes.Clear();
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
        /// Hide online status
        /// </summary>
        protected bool hidden;

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
        public WhatsApp(string phoneNum, string imei, string nick, bool debug = false, bool hidden = false)
        {
            this.messageQueue = new List<ProtocolTreeNode>();
            this.phoneNumber = phoneNum;
            this.imei = imei;
            this.name = nick;
            this.hidden = hidden;
            WhatsApp.DEBUG = debug;
            this.reader = new BinTreeNodeReader();
            this.loginStatus = CONNECTION_STATUS.DISCONNECTED;
            this.whatsNetwork = new WhatsNetwork(WhatsConstants.WhatsAppHost, WhatsConstants.WhatsPort, this.timeout);
            this.WhatsParser = new WhatsParser(this.whatsNetwork, new BinTreeNodeWriter());
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
            try
            {
                this.whatsNetwork.Connect();
                //success
                if (this.OnConnectSuccess != null)
                {
                    this.OnConnectSuccess();
                }
            }
            catch (Exception e)
            {
                if (this.OnConnectFailed != null)
                {
                    this.OnConnectFailed(e);
                }
            }
        }

        /// <summary>
        /// Disconnect from the whatsapp network
        /// </summary>
        public void Disconnect(Exception ex = null)
        {
            this.whatsNetwork.Disconenct();
            this.loginStatus = CONNECTION_STATUS.DISCONNECTED;
            if (this.OnDisconnect != null)
            {
                this.OnDisconnect(ex);
            }
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
            this.WhatsSendHandler.SendGetStatus(GetJID(jid));
        }

        public void PresenceSubscription(string target)
        {
            this.WhatsSendHandler.SendPresenceSubscriptionRequest(GetJID(target));
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
            this.reader.Key = null;
            this.WhatsSendHandler.BinWriter.Key = null;
            this._challengeBytes = null;

            string resource = string.Format(@"{0}-{1}-{2}",
                WhatsConstants.Device,
                WhatsConstants.WhatsAppVer,
                WhatsConstants.WhatsPort);
            var data = this.WhatsSendHandler.BinWriter.StartStream(WhatsConstants.WhatsAppServer, resource);
            var feat = this.addFeatures();
            var auth = this.addAuth();
            this.whatsNetwork.SendData(data);
            this.whatsNetwork.SendData(this.WhatsSendHandler.BinWriter.Write(feat, false));
            this.whatsNetwork.SendData(this.WhatsSendHandler.BinWriter.Write(auth, false));
            this.PollMessages();
            ProtocolTreeNode authResp = this.addAuthResponse();
            this.whatsNetwork.SendData(this.WhatsSendHandler.BinWriter.Write(authResp, false));
            int cnt = 0;
            do
            {
                this.PollMessages();
                System.Threading.Thread.Sleep(50);
            } 
            while ((cnt++ < 100) && (this.loginStatus == CONNECTION_STATUS.DISCONNECTED));

            this.sendNickname(this.name);
        }

        /// <summary>
        /// Send a message to a person
        /// </summary>
        /// <param name="to">The phone number to send</param>
        /// <param name="txt">The text that needs to be send</param>
        public void Message(string to, string txt)
        {
            var tmpMessage = new FMessage(GetJID(to), true) { data = txt };
            this.WhatsParser.WhatsSendHandler.SendMessage(tmpMessage, this.hidden);
        }

        public void SendSync(string[] numbers, string mode = "full", string context = "registration", int index = 0, bool last = true)
        {
            List<ProtocolTreeNode> users = new List<ProtocolTreeNode>();
            foreach (string number in numbers)
            {
                users.Add(new ProtocolTreeNode("user", null, System.Text.Encoding.UTF8.GetBytes(number)));
            }
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new KeyValue[]
            {
                new KeyValue("to", GetJID(this.phoneNumber)),
                new KeyValue("type", "get"),
                new KeyValue("id", TicketCounter.MakeId("sendsync_")),
                new KeyValue("xmlns", "urn:xmpp:whatsapp:sync")
            }, new ProtocolTreeNode("sync", new KeyValue[]
                {
                    new KeyValue("mode", mode),
                    new KeyValue("context", context),
                    new KeyValue("sid", DateTime.Now.ToFileTimeUtc().ToString()),
                    new KeyValue("index", index.ToString()),
                    new KeyValue("last", last.ToString())
                },
                users.ToArray()
                )
            );
            this.WhatsSendHandler.SendNode(node);
        }

        /// <summary>
        /// Convert the input string to a JID if necessary
        /// </summary>
        /// <param name="target">Phonenumber or JID</param>
        public static string GetJID(string target)
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
            to = GetJID(to);
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
            UploadResponse response = this.UploadFile(filehash, "image", finfo.Length, filepath, to, type);

            if (response != null && !String.IsNullOrEmpty(response.url))
            {
                //send message
                FMessage msg = new FMessage(to, true) { media_wa_type = FMessage.Type.Image, media_mime_type = response.mimetype, media_name = response.url.Split('/').Last(), media_size = response.size, media_url = response.url, binary_data = this.CreateThumbnail(filepath) };
                this.WhatsSendHandler.SendMessage(msg);
            }

        }

        public void MessageVideo(string to, string filepath)
        {
            to = GetJID(to);
            FileInfo finfo = new FileInfo(filepath);
            string type = string.Empty;
            switch (finfo.Extension)
            {
                case ".mov":
                    type = "video/quicktime";
                    break;
                case ".avi":
                    type = "video/x-msvideo";
                    break;
                default:
                    type = "video/mp4";
                    break;
            }

            //create hash
            string filehash = string.Empty;
            using (FileStream fs = File.OpenRead(filepath))
            {
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (HashAlgorithm sha = HashAlgorithm.Create("sha256"))
                    {
                        byte[] raw = sha.ComputeHash(bs);
                        filehash = Convert.ToBase64String(raw);
                    }
                }
            }

            //request upload
            UploadResponse response = this.UploadFile(filehash, "video", finfo.Length, filepath, to, type);

            if (response != null && !String.IsNullOrEmpty(response.url))
            {
                //send message
                FMessage msg = new FMessage(to, true) { media_wa_type = FMessage.Type.Video, media_mime_type = response.mimetype, media_name = response.url.Split('/').Last(), media_size = response.size, media_url = response.url, media_duration_seconds = response.duration };
                this.WhatsSendHandler.SendMessage(msg);
            }
        }

        public void MessageAudio(string to, string filepath)
        {
            to = GetJID(to);
            FileInfo finfo = new FileInfo(filepath);
            string type = string.Empty;
            switch (finfo.Extension)
            {
                case ".wav":
                    type = "audio/wav";
                    break;
                case ".ogg":
                    type = "audio/ogg";
                    break;
                case ".aif":
                    type = "audio/x-aiff";
                    break;
                case ".aac":
                    type = "audio/aac";
                    break;
                case ".m4a":
                    type = "audio/mp4";
                    break;
                default:
                    type = "audio/mpeg";
                    break;
            }

            //create hash
            string filehash = string.Empty;
            using (FileStream fs = File.OpenRead(filepath))
            {
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (HashAlgorithm sha = HashAlgorithm.Create("sha256"))
                    {
                        byte[] raw = sha.ComputeHash(bs);
                        filehash = Convert.ToBase64String(raw);
                    }
                }
            }

            //request upload
            UploadResponse response = this.UploadFile(filehash, "audio", finfo.Length, filepath, to, type);

            if (response != null && !String.IsNullOrEmpty(response.url))
            {
                //send message
                FMessage msg = new FMessage(to, true) { media_wa_type = FMessage.Type.Audio, media_mime_type = response.mimetype, media_name = response.url.Split('/').Last(), media_size = response.size, media_url = response.url, media_duration_seconds = response.duration };
                this.WhatsSendHandler.SendMessage(msg);
            }
        }

        private byte[] CreateThumbnail(string path)
        {
            if (File.Exists(path))
            {
                Image orig = Image.FromFile(path);
                if (orig != null)
                {
                    int newHeight = 0;
                    int newWidth = 0;
                    float imgWidth = float.Parse(orig.Width.ToString());
                    float imgHeight = float.Parse(orig.Height.ToString());
                    if (orig.Width > orig.Height)
                    {
                        newHeight = (int)((imgHeight / imgWidth) * 100);
                        newWidth = 100;
                    }
                    else
                    {
                        newWidth = (int)((imgWidth / imgHeight) * 100);
                        newHeight = 100;
                    }

                    Bitmap newImage = new Bitmap(newWidth, newHeight);
                    using(Graphics gr = Graphics.FromImage(newImage))
                    {
                        gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        gr.DrawImage(orig, new Rectangle(0, 0, newWidth, newHeight));
                    }
                    MemoryStream ms = new MemoryStream();
                    newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    ms.Close();
                    return ms.ToArray();
                }
            }
            return null;
        }

        private UploadResponse UploadFile(string b64hash, string type, long size, string path, string to, string contenttype)
        {
            ProtocolTreeNode media = new ProtocolTreeNode("media", new KeyValue[] {
                new KeyValue("hash", b64hash),
                new KeyValue("type", type),
                new KeyValue("size", size.ToString())
            });
            string id = TicketManager.GenerateId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new KeyValue[] {
                new KeyValue("id", id),
                new KeyValue("to", WhatsConstants.WhatsAppServer),
                new KeyValue("type", "set"),
                new KeyValue("xmlns", "w:m")
            }, media);
            this.uploadResponse = null;
            this.WhatsSendHandler.SendNode(node);
            int i = 0;
            while (this.uploadResponse == null && i < 5)
            {
                i++;
                this.PollMessages();
            }
            if (this.uploadResponse != null && this.uploadResponse.GetChild("duplicate") != null)
            {
                UploadResponse res = new UploadResponse(this.uploadResponse);
                this.uploadResponse = null;
                return res;
            }
            else
            {
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
                    sb.AppendFormat("--{0}\r\n", boundary);
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

                    List<byte> buf = new List<byte>();
                    buf.AddRange(Encoding.UTF8.GetBytes(post));
                    buf.AddRange(Encoding.UTF8.GetBytes(header));
                    buf.AddRange(File.ReadAllBytes(path));
                    buf.AddRange(Encoding.UTF8.GetBytes(footer));

                    ssl.Write(buf.ToArray(), 0, buf.ToArray().Length);

                    //moment of truth...
                    buff = new byte[1024];
                    ssl.Read(buff, 0, 1024);

                    string result = Encoding.UTF8.GetString(buff);
                    foreach (string line in result.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.StartsWith("{"))
                        {
                            string fooo = line.TrimEnd(new char[] { (char)0 });
                            JavaScriptSerializer jss = new JavaScriptSerializer();
                            UploadResponse resp = jss.Deserialize<UploadResponse>(fooo);
                            if (!String.IsNullOrEmpty(resp.url))
                            {
                                return resp;
                            }
                        }
                    }
                }
                catch (Exception)
                { }
            }
            return null;
        }

        public class UploadResponse
        {
            public string url { get; set; }
            public string mimetype { get; set; }
            public int size { get; set; }
            public string filehash { get; set; }
            public string type { get; set; }
            public int width { get; set; }
            public int height { get; set; }

            public int duration { get; set; } 
            public string acodec { get; set; }
            public int asampfreq { get; set; }
            public string asampfmt { get; set; }
            public int abitrate { get; set; }

            public UploadResponse()
            { }

            public UploadResponse(ProtocolTreeNode node)
            {
                node = node.GetChild("duplicate");
                if (node != null)
                {
                    int oSize, oWidth, oHeight, oDuration, oAsampfreq, oAbitrate;
                    this.url = node.GetAttribute("url");
                    this.mimetype = node.GetAttribute("mimetype");
                    Int32.TryParse(node.GetAttribute("size"), out oSize);
                    this.filehash = node.GetAttribute("filehash");
                    this.type = node.GetAttribute("type");
                    Int32.TryParse(node.GetAttribute("width"), out oWidth);
                    Int32.TryParse(node.GetAttribute("height"), out oHeight);
                    Int32.TryParse(node.GetAttribute("duration"), out oDuration);
                    this.acodec = node.GetAttribute("acodec");
                    Int32.TryParse(node.GetAttribute("asampfreq"), out oAsampfreq);
                    this.asampfmt = node.GetAttribute("asampfmt");
                    Int32.TryParse(node.GetAttribute("abitrate"), out oAbitrate);
                    this.size = oSize;
                    this.width = oWidth;
                    this.height = oHeight;
                    this.duration = oDuration;
                    this.asampfreq = oAsampfreq;
                    this.abitrate = oAbitrate;
                }
            }
        }

        /// <summary>
        /// Retrieve messages from the server
        /// </summary>
        public void PollMessages(bool autoReceipt = true)
        {
            this.processInboundData(autoReceipt);
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
            this.WhatsParser.WhatsSendHandler.SendQueryLastOnline(GetJID(jid));
        }

        /// <summary>
        /// Send nickname
        /// </summary>
        /// <param name="nickname">The nickname</param>
        public void sendNickname(string nickname)
        {
            this.WhatsParser.WhatsSendHandler.SendAvailableForChat(nickname, this.hidden);
        }

        /// <summary>
        /// Send unavailable status
        /// </summary>
        public void sendOffline()
        {
            this.WhatsParser.WhatsSendHandler.SendUnavailable();
        }

        public void SetPhoto(byte[] imageBytes, byte[] thumbnailBytes)
        {
            this.WhatsSendHandler.SendSetPhoto(GetJID(this.phoneNumber), imageBytes, thumbnailBytes);
        }

        /// <summary>
        /// Add the authenication nodes
        /// </summary>
        /// <returns>An instance of the ProtocolTreeNode class</returns>
        protected ProtocolTreeNode addAuth()
        {
            var node = new ProtocolTreeNode("auth",
                new KeyValue[] { 
                    new KeyValue("passive", this.hidden?"true":"false"),
                    new KeyValue("xmlns", @"urn:ietf:params:xml:ns:xmpp-sasl"),
                    new KeyValue("mechanism", Helper.KeyStream.AuthMethod),
                    new KeyValue("user", this.phoneNumber)
                });
            return node;
        }

        public void sendDeleteAccount()
        {
            this.WhatsSendHandler.SendDeleteAccount();
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

            if (this._challengeBytes != null)
            {
                byte[][] keys = KeyStream.GenerateKeys(this.encryptPassword(), this._challengeBytes);

                this.reader.Key = new KeyStream(keys[2], keys[3]);
                this.WhatsSendHandler.BinWriter.Key = new KeyStream(keys[0], keys[1]);

                List<byte> b = new List<byte>();
                b.AddRange(new byte[] { 0, 0, 0, 0 });
                b.AddRange(WhatsApp.SYSEncoding.GetBytes(this.phoneNumber));
                b.AddRange(this._challengeBytes);


                byte[] data = b.ToArray();
                this.WhatsSendHandler.BinWriter.Key.EncodeMessage(data, 0, 4, data.Length - 4);
                var node = new ProtocolTreeNode("response",
                    new KeyValue[] { new KeyValue("xmlns", "urn:ietf:params:xml:ns:xmpp-sasl") },
                    data);

                return node;
            }
            throw new Exception("Auth response error");
        }

        /// <summary>
        /// Add stream features
        /// </summary>
        /// <returns></returns>
        protected ProtocolTreeNode addFeatures()
        {
            return new ProtocolTreeNode("stream:features", null);
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
        protected void processInboundData(bool autoReceipt = true)
        {
            try
            {
                byte[] msgdata = this.whatsNetwork.ReadNextNode();
                ProtocolTreeNode node = this.reader.nextTree(msgdata);
                while (node != null)
                {
                    if (node.tag == "iq"
                    && node.GetAttribute("type") == "error")
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(node.GetAttribute("id"), node.GetAttribute("from"), Int32.Parse(node.GetChild("error").GetAttribute("code")), node.GetChild("error").GetAttribute("text"));
                        }
                    }
                    if (ProtocolTreeNode.TagEquals(node, "challenge"))
                    {
                        this.processChallenge(node);
                    }
                    else if (ProtocolTreeNode.TagEquals(node, "success"))
                    {
                        this.loginStatus = CONNECTION_STATUS.LOGGEDIN;
                        this.accountinfo = new AccountInfo(node.GetAttribute("status"),
                                                           node.GetAttribute("kind"),
                                                           node.GetAttribute("creation"),
                                                           node.GetAttribute("expiration"));
                        if (this.OnLoginSuccess != null)
                        {
                            this.OnLoginSuccess(node.GetData());
                        }
                    }
                    else if (ProtocolTreeNode.TagEquals(node, "failure"))
                    {
                        this.loginStatus = CONNECTION_STATUS.UNAUTHORIZED;
                        if (this.OnLoginFailed != null)
                        {
                            this.OnLoginFailed(node.children.FirstOrDefault().tag);
                        }
                    }
                    if (ProtocolTreeNode.TagEquals(node, "message"))
                    {
                        if (!string.IsNullOrEmpty(node.GetAttribute("notify")))
                        {
                            string name = node.GetAttribute("notify");
                            if (this.OnGetContactName != null)
                            {
                                this.OnGetContactName(node.GetAttribute("from"), name);
                            }
                        }
                        if (node.GetAttribute("type") == "error")
                        {
                            throw new NotImplementedException(node.NodeString());
                        }
                        if (node.GetChild("body") != null)
                        {
                            //text message
                            if (this.OnGetMessage != null)
                            {
                                this.OnGetMessage(node, node.GetAttribute("from"), node.GetAttribute("id"), node.GetAttribute("notify"), System.Text.Encoding.UTF8.GetString(node.GetChild("body").GetData()), autoReceipt);
                            }
                            if (autoReceipt)
                            {
                                this.sendMessageReceived(node);
                            }
                        }
                        if (node.GetChild("media") != null)
                        {
                            ProtocolTreeNode media = node.GetChild("media");
                            //media message

                            //define variables in switch
                            string file, url, from, id;
                            int size;
                            byte[] preview, dat;
                            id = node.GetAttribute("id");
                            from = node.GetAttribute("from");
                            switch (media.GetAttribute("type"))
                            {
                                case "image":
                                    if (this.OnGetMessageImage != null)
                                    {
                                        url = media.GetAttribute("url");
                                        file = media.GetAttribute("file");
                                        size = Int32.Parse(media.GetAttribute("size"));
                                        preview = media.GetData();
                                        this.OnGetMessageImage(from, id, file, size, url, preview);
                                    }
                                    break;
                                case "audio":
                                    if (this.OnGetMessageAudio != null)
                                    {
                                        file = media.GetAttribute("file");
                                        size = Int32.Parse(media.GetAttribute("size"));
                                        url = media.GetAttribute("url");
                                        preview = media.GetData();
                                        this.OnGetMessageAudio(from, id, file, size, url, preview);
                                    }
                                    break;
                                case "video":
                                    if (this.OnGetMessageVideo != null)
                                    {
                                        file = media.GetAttribute("file");
                                        size = Int32.Parse(media.GetAttribute("size"));
                                        url = media.GetAttribute("url");
                                        preview = media.GetData();
                                        this.OnGetMessageVideo(from, id, file, size, url, preview);
                                    }
                                    break;
                                case "location":
                                    if (this.OnGetMessageLocation != null)
                                    {
                                        double lon = double.Parse(media.GetAttribute("longitude"), System.Globalization.CultureInfo.InvariantCulture);
                                        double lat = double.Parse(media.GetAttribute("latitude"), System.Globalization.CultureInfo.InvariantCulture);
                                        preview = media.GetData();
                                        name = media.GetAttribute("name");
                                        url = media.GetAttribute("url");
                                        this.OnGetMessageLocation(from, id, lon, lat, url, name, preview);
                                    }
                                    break;
                                case "vcard":
                                    if (this.OnGetMessageVcard != null)
                                    {
                                        ProtocolTreeNode vcard = media.GetChild("vcard");
                                        name = vcard.GetAttribute("name");
                                        dat = vcard.GetData();
                                        this.OnGetMessageVcard(from, id, name, dat);
                                    }
                                    break;
                            }
                            this.sendMessageReceived(node);
                        }
                    }
                    if (ProtocolTreeNode.TagEquals(node, "iq"))
                    {
                        if (node.GetChild("sync") != null)
                        {
                            //sync result
                            ProtocolTreeNode sync = node.GetChild("sync");
                            ProtocolTreeNode existing = sync.GetChild("in");
                            ProtocolTreeNode nonexisting = sync.GetChild("out");
                            //process existing first
                            Dictionary<string, string> existingUsers = new Dictionary<string, string>();
                            foreach (ProtocolTreeNode child in existing.GetAllChildren())
                            {
                                existingUsers.Add(System.Text.Encoding.UTF8.GetString(child.GetData()), child.GetAttribute("jid"));
                            }
                            //now process failed numbers
                            List<string> failedNumbers = new List<string>();
                            foreach (ProtocolTreeNode child in nonexisting.GetAllChildren())
                            {
                                failedNumbers.Add(System.Text.Encoding.UTF8.GetString(child.GetData()));
                            }
                            int index = 0;
                            Int32.TryParse(sync.GetAttribute("index"), out index);
                            if (this.OnGetSyncResult != null)
                            {
                                this.OnGetSyncResult(index, sync.GetAttribute("sid"), existingUsers, failedNumbers.ToArray());
                            }
                        }
                        if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                            && node.children.FirstOrDefault().tag == "query"
                            && node.children.FirstOrDefault().GetAttribute("xmlns") == "jabber:iq:last"
                        )
                        {
                            //last seen
                            DateTime lastSeen = DateTime.Now.AddSeconds(double.Parse(node.children.FirstOrDefault().GetAttribute("seconds")) * -1);
                            if (this.OnGetLastSeen != null)
                            {
                                this.OnGetLastSeen(node.GetAttribute("from"), lastSeen);
                            }
                        }
                        if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                            && (ProtocolTreeNode.TagEquals(node.children.FirstOrDefault(), "media") || ProtocolTreeNode.TagEquals(node.children.FirstOrDefault(), "duplicate"))
                            )
                        {
                            //media upload
                            this.uploadResponse = node;
                        }
                        if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                            && ProtocolTreeNode.TagEquals(node.children.FirstOrDefault(), "picture")
                            )
                        {
                            //profile picture
                            string from = node.GetAttribute("from");
                            string id = node.GetChild("picture").GetAttribute("id");
                            byte[] dat = node.GetChild("picture").GetData();
                            string type = node.GetChild("picture").GetAttribute("type");
                            if (type == "preview")
                            {
                                if (this.OnGetPhotoPreview != null)
                                {
                                    this.OnGetPhotoPreview(from, id, dat);
                                }
                            }
                            else
                            {
                                if (this.OnGetPhoto != null)
                                {
                                    this.OnGetPhoto(from, id, dat);
                                }
                            }
                        }
                        if (node.GetAttribute("type").Equals("get", StringComparison.OrdinalIgnoreCase)
                            && ProtocolTreeNode.TagEquals(node.children.FirstOrDefault(), "ping"))
                        {
                            this.Pong(node.GetAttribute("id"));
                        }
                        if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                            && ProtocolTreeNode.TagEquals(node.children.FirstOrDefault(), "group"))
                        {
                            //group(s) info
                            List<GroupInfo> groups = new List<GroupInfo>();
                            foreach (ProtocolTreeNode group in node.children)
                            {
                                groups.Add(new GroupInfo(
                                    group.GetAttribute("id"),
                                    group.GetAttribute("owner"),
                                    long.Parse(group.GetAttribute("creation")),
                                    group.GetAttribute("subject"),
                                    long.Parse(group.GetAttribute("s_t")),
                                    group.GetAttribute("s_o")
                                    ));
                            }
                            if (this.OnGetGroups != null)
                            {
                                this.OnGetGroups(groups.ToArray());
                            }
                        }
                        if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                            && ProtocolTreeNode.TagEquals(node.children.FirstOrDefault(), "participant"))
                        {
                            //group participants
                            List<string> participants = new List<string>();
                            foreach (ProtocolTreeNode part in node.GetAllChildren())
                            {
                                if (part.tag == "participant" && !string.IsNullOrEmpty(part.GetAttribute("jid")))
                                {
                                    participants.Add(part.GetAttribute("jid"));
                                }
                            }
                            if (this.OnGetGroupParticipants != null)
                            {
                                this.OnGetGroupParticipants(node.GetAttribute("from"), participants.ToArray());
                            }
                        }
                    }

                    if (ProtocolTreeNode.TagEquals(node, "stream:error"))
                    {
                        var textNode = node.GetChild("text");
                        if (textNode != null)
                        {
                            string content = WhatsApp.SYSEncoding.GetString(textNode.GetData());
                            Console.WriteLine("Error : " + content);
                            if (content.Equals("Replaced by new connection", StringComparison.OrdinalIgnoreCase))
                            {
                                this.Disconnect(new Exception(content));
                                this.Connect();
                                this.Login();
                            }
                        }
                    }
                    if (ProtocolTreeNode.TagEquals(node, "presence"))
                    {
                        //presence node
                        if (this.OnGetPresence != null)
                        {
                            this.OnGetPresence(node.GetAttribute("from"), node.GetAttribute("type"));
                        }
                    }

                    if (node.tag == "ib")
                    {
                        foreach (ProtocolTreeNode child in node.children)
                        {
                            switch (child.tag)
                            {
                                case "dirty":
                                    this.WhatsSendHandler.SendClearDirty(child.GetAttribute("type"));
                                    break;
                                case "offline":
                                    //this.SendQrSync(null);
                                    break;
                                default:
                                    throw new NotImplementedException(node.NodeString());
                            }
                        }
                    }

                    if (node.tag == "chatstate")
                    {
                        string state = node.children.FirstOrDefault().tag;
                        switch (state)
                        {
                            case "composing":
                                if (this.OnGetTyping != null)
                                {
                                    this.OnGetTyping(node.GetAttribute("from"));
                                }
                                break;
                            case "paused":
                                if (this.OnGetPaused != null)
                                {
                                    this.OnGetPaused(node.GetAttribute("from"));
                                }
                                break;
                            default:
                                throw new NotImplementedException(node.NodeString());
                        }
                    }

                    if (node.tag == "ack")
                    {
                        //do nothing
                        //throw new NotImplementedException(node.NodeString());
                    }

                    if (node.tag == "notification")
                    {
                        if(!String.IsNullOrEmpty(node.GetAttribute("notify")))
                        {
                            if(this.OnGetContactName != null)
                            {
                                this.OnGetContactName(node.GetAttribute("from"), node.GetAttribute("notify"));
                            }
                        }
                        string type = node.GetAttribute("type");
                        switch(type)
                        {
                            case "picture":
                                ProtocolTreeNode child = node.children.FirstOrDefault();
                                if (this.OnNotificationPicture != null)
                                {
                                    this.OnNotificationPicture(child.tag, child.GetAttribute("jid"), child.GetAttribute("id"));
                                }
                                break;
                            case "status":
                                ProtocolTreeNode child2 = node.children.FirstOrDefault();
                                if (this.OnGetStatus != null)
                                {
                                    this.OnGetStatus(node.GetAttribute("from"), child2.tag, node.GetAttribute("notify"), System.Text.Encoding.UTF8.GetString(child2.GetData()));
                                }
                                break;
                            default:
                                throw new NotImplementedException(node.NodeString());
                        }
                        this.SendNotificationAck(node);
                    }
                    node = this.reader.nextTree();
                }
            }
            catch (Exception e)
            {
                //whatever
                this._incompleteBytes.Clear();
            }
        }

        private void SendNotificationAck(ProtocolTreeNode node)
        {
            string from = node.GetAttribute("from");
            string to = node.GetAttribute("to");
            string participant = node.GetAttribute("participant");
            string id = node.GetAttribute("id");
            string type = node.GetAttribute("type");
            List<KeyValue> attributes = new List<KeyValue>();
            if(!string.IsNullOrEmpty(to))
            {
                attributes.Add(new KeyValue("from", to));
            }
            if(!string.IsNullOrEmpty(participant))
            {
                attributes.Add(new KeyValue("participant", participant));
            }
            attributes.AddRange(new [] {
                new KeyValue("to", from),
                new KeyValue("class", "notification"),
                new KeyValue("id", id),
                new KeyValue("type", type)
            });
            ProtocolTreeNode sendNode = new ProtocolTreeNode("ack", attributes.ToArray());
            this.WhatsSendHandler.SendNode(sendNode);
        }

        protected void SendQrSync(byte[] qrkey, byte[] token = null)
        {
            string id = TicketCounter.MakeId("qrsync_");
            List<ProtocolTreeNode> children = new List<ProtocolTreeNode>();
            children.Add(new ProtocolTreeNode("sync", null, qrkey));
            if (token != null)
            {
                children.Add(new ProtocolTreeNode("code", null, token));
            }
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] {
                new KeyValue("type", "set"),
                new KeyValue("id", id),
                new KeyValue("xmlns", "w:web")
            }, children.ToArray());
            this.WhatsSendHandler.SendNode(node);
        }

        /// <summary>
        /// Tell the server we recieved the message
        /// </summary>
        /// <param name="msg">The ProtocolTreeNode that contains the message</param>
        public void sendMessageReceived(ProtocolTreeNode msg, string response = "received")
        {
            FMessage tmpMessage = new FMessage(new FMessage.FMessageIdentifierKey(msg.GetAttribute("from"), true, msg.GetAttribute("id")));
            this.WhatsParser.WhatsSendHandler.SendMessageReceived(tmpMessage, response);
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





        // events
        public event NullDelegate OnConnectSuccess;
        public event ExceptionDelegate OnConnectFailed;
        public event ExceptionDelegate OnDisconnect;
        public event ByteArrayDelegate OnLoginSuccess;
        public event StringDelegate OnLoginFailed;

        public event OnGetMessageDelegate OnGetMessage;
        public event OnGetMediaDelegate OnGetMessageImage;
        public event OnGetMediaDelegate OnGetMessageVideo;
        public event OnGetMediaDelegate OnGetMessageAudio;
        public event OnGetLocationDelegate OnGetMessageLocation;
        public event OnGetVcardDelegate OnGetMessageVcard;

        public event OnErrorDelegate OnError;
        public event OnNotificationPictureDelegate OnNotificationPicture;
        
        public event OnGetMessageReceivedDelegate OnGetMessageReceivedServer;
        public event OnGetMessageReceivedDelegate OnGetMessageReceivedClient;

        public event OnGetPresenceDelegate OnGetPresence;
        public event OnGetGroupParticipantsDelegate OnGetGroupParticipants;
        public event OnGetLastSeenDelegate OnGetLastSeen;
        public event OnGetchatStateDelegate OnGetTyping;
        public event OnGetchatStateDelegate OnGetPaused;
        public event OnGetPictureDelegate OnGetPhoto;
        public event OnGetPictureDelegate OnGetPhotoPreview;
        public event OnGetGroupsDelegate OnGetGroups;
        public event OnContactNameDelegate OnGetContactName;
        public event OnGetStatusDelegate OnGetStatus;

        public event OnGetSyncResultDelegate OnGetSyncResult;

        //event delegates
        public delegate void OnContactNameDelegate(string from, string contactName);
        public delegate void NullDelegate();
        public delegate void ExceptionDelegate(Exception ex);
        public delegate void ByteArrayDelegate(byte[] data);
        public delegate void StringDelegate(string data);
        public delegate void OnErrorDelegate(string id, string from, int code, string text);
        public delegate void OnGetMessageReceivedDelegate(string from, string id);
        public delegate void OnNotificationPictureDelegate(string type, string jid, string id);
        public delegate void OnGetMessageDelegate(ProtocolTreeNode messageNode, string from, string id, string name, string message, bool receipt_sent);
        public delegate void OnGetPresenceDelegate(string from, string type);
        public delegate void OnGetGroupParticipantsDelegate(string gjid, string[] jids);
        public delegate void OnGetLastSeenDelegate(string from, DateTime lastSeen);
        public delegate void OnGetchatStateDelegate(string from);
        public delegate void OnGetMediaDelegate(string from, string id, string fileName, int fileSize, string url, byte[] preview);
        public delegate void OnGetLocationDelegate(string from, string id, double lon, double lat, string url, string name, byte[] preview);
        public delegate void OnGetVcardDelegate(string from, string id, string name, byte[] data);
        public delegate void OnGetPictureDelegate(string from, string id, byte[] data);
        public delegate void OnGetGroupsDelegate(GroupInfo[] groups);
        public delegate void OnGetStatusDelegate(string from, string type, string name, string status);
        public delegate void OnGetSyncResultDelegate(int index, string sid, Dictionary<string, string> existingUsers, string[] failedNumbers);

        public class GroupInfo
        {
            public readonly string id;
            public readonly string owner;
            public readonly long creation;
            public readonly string subject;
            public readonly long subjectChangedTime;
            public readonly string subjectChangedBy;

            internal GroupInfo(string id, string owner, long creation, string subject, long subjectChanged, string subjectChangedBy)
            {
                this.id = id;
                this.owner = owner;
                this.creation = creation;
                this.subject = subject;
                this.subjectChangedTime = subjectChanged;
                this.subjectChangedBy = subjectChangedBy;
            }
        }
    }
}
