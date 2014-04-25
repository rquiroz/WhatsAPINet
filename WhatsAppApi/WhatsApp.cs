using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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

        protected KeyStream outputKey;

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
        /// Holds the encoding we use, default is UTF8
        /// </summary>
        public static readonly Encoding SYSEncoding = Encoding.UTF8;

        /// <summary>
        /// Empty bytes to hold the challenge
        /// </summary>
        private byte[] _challengeBytes;

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
            this.BinWriter = new BinTreeNodeWriter();
            this.whatsNetwork = new WhatsNetwork(WhatsConstants.WhatsAppHost, WhatsConstants.WhatsPort, this.timeout);
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
                this.loginStatus = CONNECTION_STATUS.CONNECTED;
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
        public void Login(byte[] nextChallenge = null)
        {
            //reset stuff
            this.reader.Key = null;
            this.BinWriter.Key = null;
            this._challengeBytes = null;

            if (nextChallenge != null)
            {
                this._challengeBytes = nextChallenge;
            }

            string resource = string.Format(@"{0}-{1}-{2}",
                WhatsConstants.Device,
                WhatsConstants.WhatsAppVer,
                WhatsConstants.WhatsPort);
            var data = this.BinWriter.StartStream(WhatsConstants.WhatsAppServer, resource);
            var feat = this.addFeatures();
            var auth = this.addAuth();
            this.SendData(data);
            this.SendData(this.BinWriter.Write(feat, false));
            this.SendData(this.BinWriter.Write(auth, false));

            this.pollMessage();//stream start
            this.pollMessage();//features
            this.pollMessage();//challenge or success

            if (this.loginStatus != CONNECTION_STATUS.LOGGEDIN)
            {
                //oneshot failed
                ProtocolTreeNode authResp = this.addAuthResponse();
                this.SendData(this.BinWriter.Write(authResp, false));
                this.pollMessage();
            }

            this.SendAvailableForChat(this.name, this.hidden);
        }

        protected void SendData(byte[] data)
        {
            try
            {
                this.whatsNetwork.SendData(data);
            }
            catch (ConnectionException)
            {
                this.Disconnect();
            }
        }

        /// <summary>
        /// Send a message to a person
        /// </summary>
        /// <param name="to">The phone number to send</param>
        /// <param name="txt">The text that needs to be send</param>
        public void Message(string to, string txt)
        {
            var tmpMessage = new FMessage(GetJID(to), true) { data = txt };
            this.SendMessage(tmpMessage, this.hidden);
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
            this.SendNode(node);
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
                this.SendMessage(msg);
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
                this.SendMessage(msg);
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
                this.SendMessage(msg);
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
            this.SendNode(node);
            int i = 0;
            while (this.uploadResponse == null && i <= 10)
            {
                i++;
                this.pollMessage();
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
            while (pollMessage(autoReceipt)) ;
        }

        public bool pollMessage(bool autoReceipt = true)
        {
            if (this.loginStatus == CONNECTION_STATUS.CONNECTED || this.loginStatus == CONNECTION_STATUS.LOGGEDIN)
            {
                byte[] nodeData;
                try
                {
                    nodeData = this.whatsNetwork.ReadNextNode();
                    if (nodeData != null)
                    {
                        return this.processInboundData(nodeData, autoReceipt);
                    }
                }
                catch (ConnectionException)
                {
                    this.Disconnect();
                }
            }
            return false;
        }

        /// <summary>
        /// Add the authenication nodes
        /// </summary>
        /// <returns>An instance of the ProtocolTreeNode class</returns>
        protected ProtocolTreeNode addAuth()
        {
            List<KeyValue> attr = new List<KeyValue>(new KeyValue[] {
                new KeyValue("mechanism", Helper.KeyStream.AuthMethod),
                new KeyValue("user", this.phoneNumber)});
            if (this.hidden)
            {
                attr.Add(new KeyValue("passive", "true"));
            }
            var node = new ProtocolTreeNode("auth", attr.ToArray(), null, this.getAuthBlob());
            return node;
        }

        protected byte[] getAuthBlob()
        {
            byte[] data = null;
            if (this._challengeBytes != null)
            {
                byte[][] keys = KeyStream.GenerateKeys(this.encryptPassword(), this._challengeBytes);

                this.reader.Key = new KeyStream(keys[2], keys[3]);

                this.outputKey = new KeyStream(keys[0], keys[1]);

                PhoneNumber pn = new PhoneNumber(this.phoneNumber);

                List<byte> b = new List<byte>();
                b.AddRange(new byte[] { 0, 0, 0, 0 });
                b.AddRange(WhatsApp.SYSEncoding.GetBytes(this.phoneNumber));
                b.AddRange(this._challengeBytes);
                b.AddRange(WhatsApp.SYSEncoding.GetBytes(Helper.Func.GetNowUnixTimestamp().ToString()));
                b.AddRange(WhatsApp.SYSEncoding.GetBytes(WhatsConstants.UserAgent));
                b.AddRange(WhatsApp.SYSEncoding.GetBytes(String.Format(" MccMnc/{0}001", pn.MCC)));
                data = b.ToArray();

                this._challengeBytes = null;

                this.outputKey.EncodeMessage(data, 0, 4, data.Length - 4);

                this.BinWriter.Key = this.outputKey;
            }

            return data;
        }

        /// <summary>
        /// Add the auth response to protocoltreenode
        /// </summary>
        /// <returns>An instance of the ProtocolTreeNode</returns>
        protected ProtocolTreeNode addAuthResponse()
        {
            if (this._challengeBytes != null)
            {
                byte[][] keys = KeyStream.GenerateKeys(this.encryptPassword(), this._challengeBytes);

                this.reader.Key = new KeyStream(keys[2], keys[3]);
                this.BinWriter.Key = new KeyStream(keys[0], keys[1]);

                List<byte> b = new List<byte>();
                b.AddRange(new byte[] { 0, 0, 0, 0 });
                b.AddRange(WhatsApp.SYSEncoding.GetBytes(this.phoneNumber));
                b.AddRange(this._challengeBytes);


                byte[] data = b.ToArray();
                this.BinWriter.Key.EncodeMessage(data, 0, 4, data.Length - 4);
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
        protected bool processInboundData(byte[] msgdata, bool autoReceipt = true)
        {
            try
            {
                ProtocolTreeNode node = this.reader.nextTree(msgdata);
                if(node != null)
                {
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
                            this.OnLoginSuccess(this.phoneNumber, node.GetData());
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
                    
                    
                    
                    
                    
                    if (ProtocolTreeNode.TagEquals(node, "receipt"))
                    {
                        string from = node.GetAttribute("from");
                        string id = node.GetAttribute("id");
                        string type = node.GetAttribute("type") ?? "delivery";
                        switch (type)
                        {
                            case "delivery":
                                //delivered to target
                                if (this.OnGetMessageReceivedClient != null)
                                {
                                    this.OnGetMessageReceivedClient(from, id);
                                }
                                break;
                            case "read":
                                //read by target
                                //todo
                                break;
                            case "played":
                                //played by target
                                //todo
                                break;
                        }

                        //send ack
                        SendNotificationAck(node, type);
                    }


                    if (ProtocolTreeNode.TagEquals(node, "message"))
                    {
                        this.handleMessage(node, autoReceipt);
                    }


                    if (ProtocolTreeNode.TagEquals(node, "iq"))
                    {
                        this.handleIq(node);
                    }



                    if (ProtocolTreeNode.TagEquals(node, "stream:error"))
                    {
                        var textNode = node.GetChild("text");
                        if (textNode != null)
                        {
                            string content = WhatsApp.SYSEncoding.GetString(textNode.GetData());
                            Console.WriteLine("Error : " + content);
                        }
                        this.Disconnect();
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
                                    this.SendClearDirty(child.GetAttribute("type"));
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
                        string cls = node.GetAttribute("class");
                        if (cls == "message")
                        {
                            //server receipt
                            if (this.OnGetMessageReceivedServer != null)
                            {
                                this.OnGetMessageReceivedServer(node.GetAttribute("from"), node.GetAttribute("id"));
                            }
                        }
                        else if(cls == "receipt")
                        {
                            //message received ack
                            //do nothing
                        }
                    }



                    if (node.tag == "notification")
                    {
                        this.handleNotification(node);
                    }



                    return true;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return false;
        }

        protected void handleMessage(ProtocolTreeNode node, bool autoReceipt)
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

        protected void handleIq(ProtocolTreeNode node)
        {
            if (node.GetAttribute("type") == "error")
            {
                if (this.OnError != null)
                {
                    this.OnError(node.GetAttribute("id"), node.GetAttribute("from"), Int32.Parse(node.GetChild("error").GetAttribute("code")), node.GetChild("error").GetAttribute("text"));
                }
            }
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
                this.SendPong(node.GetAttribute("id"));
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

        protected void handleNotification(ProtocolTreeNode node)
        {
            if (!String.IsNullOrEmpty(node.GetAttribute("notify")))
            {
                if (this.OnGetContactName != null)
                {
                    this.OnGetContactName(node.GetAttribute("from"), node.GetAttribute("notify"));
                }
            }
            string type = node.GetAttribute("type");
            switch (type)
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

        private void SendNotificationAck(ProtocolTreeNode node, string type = null)
        {
            string from = node.GetAttribute("from");
            string to = node.GetAttribute("to");
            string participant = node.GetAttribute("participant");
            string id = node.GetAttribute("id");
            if (type == null)
            {
                type = node.GetAttribute("type");
            }
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
                new KeyValue("class", node.tag),
                new KeyValue("id", id),
                new KeyValue("type", type)
            });
            ProtocolTreeNode sendNode = new ProtocolTreeNode("ack", attributes.ToArray());
            this.SendNode(sendNode);
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
            this.SendNode(node);
        }

        /// <summary>
        /// Tell the server we recieved the message
        /// </summary>
        /// <param name="msg">The ProtocolTreeNode that contains the message</param>
        public void sendMessageReceived(ProtocolTreeNode msg, string response = "received")
        {
            FMessage tmpMessage = new FMessage(new FMessage.FMessageIdentifierKey(msg.GetAttribute("from"), true, msg.GetAttribute("id")));
            this.SendMessageReceived(tmpMessage, response);
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
        public event LoginSuccessDelegate OnLoginSuccess;
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
        public delegate void LoginSuccessDelegate(string phoneNumber, byte[] data);
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

        internal BinTreeNodeWriter BinWriter;

        public void SendActive()
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "active") });
            this.SendNode(node);
        }

        public void SendAddParticipants(string gjid, IEnumerable<string> participants)
        {
            this.SendAddParticipants(gjid, participants, null, null);
        }

        public void SendAddParticipants(string gjid, IEnumerable<string> participants, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("add_group_participants_");
            this.SendVerbParticipants(gjid, participants, id, "add");
        }

        public void SendAvailableForChat(string nickName, bool isHidden = false)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("name", nickName) });
            this.SendNode(node);
        }

        public void SendUnavailable()
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unavailable") });
            this.SendNode(node);
        }

        public void SendClearDirty(IEnumerable<string> categoryNames)
        {
            string id = TicketCounter.MakeId("clean_dirty_");
            List<ProtocolTreeNode> children = new List<ProtocolTreeNode>();
            foreach (string category in categoryNames)
            {
                ProtocolTreeNode cat = new ProtocolTreeNode("clean", new[] { new KeyValue("type", category) });
                children.Add(cat);
            }
            var node = new ProtocolTreeNode("iq",
                                            new[]
                                                {
                                                    new KeyValue("id", id), 
                                                    new KeyValue("type", "set"),
                                                    new KeyValue("to", "s.whatsapp.net"),
                                                    new KeyValue("xmlns", "urn:xmpp:whatsapp:dirty")
                                                }, children);
            this.SendNode(node);
        }

        public void SendClearDirty(string category)
        {
            this.SendClearDirty(new string[] { category });
        }

        public void SendClientConfig(string platform, string lg, string lc)
        {
            string v = TicketCounter.MakeId("config_");
            var child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push"), new KeyValue("platform", platform), new KeyValue("lg", lg), new KeyValue("lc", lc) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", v), new KeyValue("type", "set"), new KeyValue("to", WhatsConstants.WhatsAppRealm) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendClientConfig(string platform, string lg, string lc, Uri pushUri, bool preview, bool defaultSetting, bool groupsSetting, IEnumerable<GroupSetting> groups, Action onCompleted, Action<int> onError)
        {
            string id = TicketCounter.MakeId("config_");
            var node = new ProtocolTreeNode("iq",
                                        new[]
                                        {
                                            new KeyValue("id", id), new KeyValue("type", "set"),
                                            new KeyValue("to", "") //this.Login.Domain)
                                        },
                                        new ProtocolTreeNode[]
                                        {
                                            new ProtocolTreeNode("config",
                                            new[]
                                                {
                                                    new KeyValue("xmlns","urn:xmpp:whatsapp:push"),
                                                    new KeyValue("platform", platform),
                                                    new KeyValue("lg", lg),
                                                    new KeyValue("lc", lc),
                                                    new KeyValue("clear", "0"),
                                                    new KeyValue("id", pushUri.ToString()),
                                                    new KeyValue("preview",preview ? "1" : "0"),
                                                    new KeyValue("default",defaultSetting ? "1" : "0"),
                                                    new KeyValue("groups",groupsSetting ? "1" : "0")
                                                },
                                            this.ProcessGroupSettings(groups))
                                        });
            this.SendNode(node);
        }

        public void SendClose()
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unavailable") });
            this.SendNode(node);
        }

        public void SendComposing(string to)
        {
            this.SendChatState(to, "composing");
        }

        protected void SendChatState(string to, string type)
        {
            var node = new ProtocolTreeNode("chatstate", new[] { new KeyValue("to", WhatsApp.GetJID(to)) }, new[] { 
                new ProtocolTreeNode(type, null)
            });
            this.SendNode(node);
        }

        public void SendCreateGroupChat(string subject)
        {
            string id = TicketCounter.MakeId("create_group_");
            var child = new ProtocolTreeNode("group", new[] { new KeyValue("action", "create"), new KeyValue("subject", subject) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", "g.us") }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendDeleteAccount()
        {
            string id = TicketCounter.MakeId("del_acct_");
            var node = new ProtocolTreeNode("iq",
                                            new[]
                                                {
                                                    new KeyValue("id", id), new KeyValue("type", "get"),
                                                    new KeyValue("to", "s.whatsapp.net")
                                                },
                                            new ProtocolTreeNode[]
                                                {
                                                    new ProtocolTreeNode("remove",
                                                                         new[]
                                                                             {
                                                                                 new KeyValue("xmlns", "urn:xmpp:whatsapp:account")
                                                                             })
                                                });
            this.SendNode(node);
        }

        public void SendDeleteFromRoster(string jid)
        {
            string v = TicketCounter.MakeId("roster_");
            var innerChild = new ProtocolTreeNode("item", new[] { new KeyValue("jid", jid), new KeyValue("subscription", "remove") });
            var child = new ProtocolTreeNode("query", new[] { new KeyValue("xmlns", "jabber:iq:roster") }, new ProtocolTreeNode[] { innerChild });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("type", "set"), new KeyValue("id", v) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendDeliveredReceiptAck(string to, string id)
        {
            this.SendReceiptAck(to, id, "delivered");
        }

        public void SendEndGroupChat(string gjid)
        {
            string id = TicketCounter.MakeId("remove_group_");
            var child = new ProtocolTreeNode("group", new[] { new KeyValue("action", "delete") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", gjid) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendGetClientConfig()
        {
            string id = TicketCounter.MakeId("get_config_");
            var child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", WhatsConstants.WhatsAppRealm) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendGetDirty()
        {
            string id = TicketCounter.MakeId("get_dirty_");
            var child = new ProtocolTreeNode("status", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:dirty") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", "s.whatsapp.net") }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendGetGroupInfo(string gjid)
        {
            string id = TicketCounter.MakeId("get_g_info_");
            var child = new ProtocolTreeNode("query", null);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g"), new KeyValue("to", WhatsAppApi.WhatsApp.GetJID(gjid)) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendGetGroups()
        {
            string id = TicketCounter.MakeId("get_groups_");
            this.SendGetGroups(id, "participating");
        }

        public void SendGetOwningGroups()
        {
            string id = TicketCounter.MakeId("get_owning_groups_");
            this.SendGetGroups(id, "owning");
        }

        public void SendGetParticipants(string gjid)
        {
            string id = TicketCounter.MakeId("get_participants_");
            var child = new ProtocolTreeNode("list", null);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g"), new KeyValue("to", WhatsApp.GetJID(gjid)) }, child);
            this.SendNode(node);
        }

        public string SendGetPhoto(string jid, string expectedPhotoId, bool largeFormat)
        {
            string id = TicketCounter.MakeId("get_photo_");
            var attrList = new List<KeyValue>();
            if (!largeFormat)
            {
                attrList.Add(new KeyValue("type", "preview"));
            }
            if (expectedPhotoId != null)
            {
                attrList.Add(new KeyValue("id", expectedPhotoId));
            }
            var child = new ProtocolTreeNode("picture", attrList.ToArray());
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:profile:picture"), new KeyValue("to", WhatsAppApi.WhatsApp.GetJID(jid)) }, child);
            this.SendNode(node);
            return id;
        }

        public void SendGetPhotoIds(IEnumerable<string> jids)
        {
            string id = TicketCounter.MakeId("get_photo_id_");
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", GetJID(this.phoneNumber)) },
                new ProtocolTreeNode("list", new[] { new KeyValue("xmlns", "w:profile:picture") },
                    (from jid in jids select new ProtocolTreeNode("user", new[] { new KeyValue("jid", jid) })).ToArray<ProtocolTreeNode>()));
            this.SendNode(node);
        }

        public void SendGetPrivacyList()
        {
            string id = TicketCounter.MakeId("privacylist_");
            var innerChild = new ProtocolTreeNode("list", new[] { new KeyValue("name", "default") });
            var child = new ProtocolTreeNode("query", new KeyValue[] { new KeyValue("xmlns", "jabber:iq:privacy") }, innerChild);
            var node = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "get") }, child);
            this.SendNode(node);
        }

        public void SendGetServerProperties()
        {
            string id = TicketCounter.MakeId("get_server_properties_");
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", "s.whatsapp.net") },
                new ProtocolTreeNode("props", new[] { new KeyValue("xmlns", "w") }));
            this.SendNode(node);
        }

        public void SendGetStatuses(string[] jids)
        {
            List<ProtocolTreeNode> targets = new List<ProtocolTreeNode>();
            foreach (string jid in jids)
            {
                targets.Add(new ProtocolTreeNode("user", new[] { new KeyValue("jid", GetJID(jid)) }, null, null));
            }

            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] {
                new KeyValue("to", "s.whatsapp.net"),
                new KeyValue("type", "get"),
                new KeyValue("xmlns", "status"),
                new KeyValue("id", TicketCounter.MakeId("getstatus"))
            }, new[] {
                new ProtocolTreeNode("status", null, targets.ToArray(), null)
            }, null);

            this.SendNode(node);
        }

        public void SendInactive()
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "inactive") });
            this.SendNode(node);
        }

        public void SendLeaveGroup(string gjid)
        {
            this.SendLeaveGroups(new string[] { gjid });
        }

        public void SendLeaveGroups(IEnumerable<string> gjids)
        {
            string id = TicketCounter.MakeId("leave_group_");
            IEnumerable<ProtocolTreeNode> innerChilds = from gjid in gjids select new ProtocolTreeNode("group", new[] { new KeyValue("id", gjid) });
            var child = new ProtocolTreeNode("leave", null, innerChilds);
            var node = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", "g.us") }, child);
            this.SendNode(node);
        }

        public void SendMessage(FMessage message, bool hidden = false)
        {
            if (message.media_wa_type != FMessage.Type.Undefined)
            {
                this.SendMessageWithMedia(message);
            }
            else
            {
                this.SendMessageWithBody(message, hidden);
            }
        }

        //overload
        public void SendMessageBroadcast(string[] to, string message)
        {
            this.SendMessageBroadcast(to, new FMessage(string.Empty, true) { data = message, media_wa_type = FMessage.Type.Undefined });
        }

        //overload
        public void SendMessageBroadcast(string to, FMessage message)
        {
            this.SendMessageBroadcast(new string[] { to }, message);
        }

        //overload
        public void SendMessageBroadcast(string to, string message)
        {
            this.SendMessageBroadcast(new string[] { to }, new FMessage(string.Empty, true) { data = message, media_wa_type = FMessage.Type.Undefined });
        }

        //send broadcast
        public void SendMessageBroadcast(string[] to, FMessage message)
        {
            if (to != null && to.Length > 0 && message != null && !string.IsNullOrEmpty(message.data))
            {
                ProtocolTreeNode child;
                if (message.media_wa_type == FMessage.Type.Undefined)
                {
                    //text broadcast
                    child = new ProtocolTreeNode("body", null, null, WhatsApp.SYSEncoding.GetBytes(message.data));
                }
                else
                {
                    throw new NotImplementedException();
                }

                //compose broadcast envelope
                ProtocolTreeNode xnode = new ProtocolTreeNode("x", new KeyValue[] {
                    new KeyValue("xmlns", "jabber:x:event")
                }, new ProtocolTreeNode("server", null));
                List<ProtocolTreeNode> toNodes = new List<ProtocolTreeNode>();
                foreach (string target in to)
                {
                    toNodes.Add(new ProtocolTreeNode("to", new KeyValue[] { new KeyValue("jid", WhatsAppApi.WhatsApp.GetJID(target)) }));
                }

                ProtocolTreeNode broadcastNode = new ProtocolTreeNode("broadcast", null, toNodes);
                ProtocolTreeNode messageNode = new ProtocolTreeNode("message", new KeyValue[] {
                    new KeyValue("to", "broadcast"),
                    new KeyValue("type", "chat"),
                    new KeyValue("id", message.identifier_key.id)
                }, new ProtocolTreeNode[] {
                    broadcastNode,
                    xnode,
                    child
                });
                this.SendNode(messageNode);
            }
        }

        public void SendMessageReceived(FMessage message, string response)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("receipt", new[] {
                new KeyValue("to", message.identifier_key.remote_jid),
                new KeyValue("id", message.identifier_key.id)
            });

            this.SendNode(node);
        }

        public void SendNop()
        {
            this.SendNode(null);
        }

        public void SendNotificationReceived(string jid, string id)
        {
            var child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            var node = new ProtocolTreeNode("message", new[] { new KeyValue("to", jid), new KeyValue("type", "notification"), new KeyValue("id", id) }, child);
            this.SendNode(node);
        }

        public void SendPaused(string to)
        {
            this.SendChatState(to, "paused");
        }

        public void SendPing()
        {
            string id = TicketCounter.MakeId("ping_");
            var child = new ProtocolTreeNode("ping", new[] { new KeyValue("xmlns", "w:p") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get") }, child);
            this.SendNode(node);
        }

        public void SendPong(string id)
        {
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("type", "result"), new KeyValue("to", WhatsConstants.WhatsAppRealm), new KeyValue("id", id) });
            this.SendNode(node);
        }

        public void SendPresenceSubscriptionRequest(string to)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "subscribe"), new KeyValue("to", GetJID(to)) });
            this.SendNode(node);
        }

        public void SendQueryLastOnline(string jid)
        {
            string id = TicketCounter.MakeId("last_");
            var child = new ProtocolTreeNode("query", null);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", jid), new KeyValue("xmlns", "jabber:iq:last") }, child);
            this.SendNode(node);
        }

        public void SendRelayCapable(string platform, bool value)
        {
            string v = TicketCounter.MakeId("relay_");
            var child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push"), new KeyValue("platform", platform), new KeyValue("relay", value ? "1" : "0") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", v), new KeyValue("type", "set"), new KeyValue("to", WhatsConstants.WhatsAppRealm) }, child);
            this.SendNode(node);
        }

        public void SendRelayComplete(string id, int millis)
        {
            var child = new ProtocolTreeNode("relay", new[] { new KeyValue("elapsed", millis.ToString(CultureInfo.InvariantCulture)) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("xmlns", "w:p:r"), new KeyValue("type", "result"), new KeyValue("to", "s.whatsapp.net"), new KeyValue("id", id) }, child);
            this.SendNode(node);
        }

        public void SendRelayTimeout(string id)
        {
            var innerChild = new ProtocolTreeNode("remote-server-timeout", new[] { new KeyValue("xmlns", "urn:ietf:params:xml:ns:xmpp-stanzas") });
            var child = new ProtocolTreeNode("error", new[] { new KeyValue("code", "504"), new KeyValue("type", "wait") }, innerChild);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("xmlns", "w:p:r"), new KeyValue("type", "error"), new KeyValue("to", "s.whatsapp.net"), new KeyValue("id", id) }, child);
            this.SendNode(node);
        }

        public void SendRemoveParticipants(string gjid, List<string> participants)
        {
            string id = TicketCounter.MakeId("remove_group_participants_");
            this.SendVerbParticipants(gjid, participants, id, "remove");
        }

        public void SendSetGroupSubject(string gjid, string subject)
        {
            string id = TicketCounter.MakeId("set_group_subject_");
            var child = new ProtocolTreeNode("subject", new[] { new KeyValue("value", subject) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", gjid) }, child);
            this.SendNode(node);
        }

        public void SendSetPhoto(string jid, byte[] bytes, byte[] thumbnailBytes)
        {
            string id = TicketCounter.MakeId("set_photo_");
            var list = new List<ProtocolTreeNode> { new ProtocolTreeNode("picture", null, null, bytes) };
            if (thumbnailBytes != null)
            {
                list.Add(new ProtocolTreeNode("picture", new[] { new KeyValue("type", "preview") }, null, thumbnailBytes));
            }
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:profile:picture"), new KeyValue("to", jid) }, list.ToArray());
            this.SendNode(node);
        }

        public void SendSetPrivacyBlockedList(IEnumerable<string> jidSet)
        {
            string id = TicketCounter.MakeId("privacy_");
            ProtocolTreeNode[] nodeArray = Enumerable.Select<string, ProtocolTreeNode>(jidSet, (Func<string, int, ProtocolTreeNode>)((jid, index) => new ProtocolTreeNode("item", new KeyValue[] { new KeyValue("type", "jid"), new KeyValue("value", jid), new KeyValue("action", "deny"), new KeyValue("order", index.ToString(CultureInfo.InvariantCulture)) }))).ToArray<ProtocolTreeNode>();
            var child = new ProtocolTreeNode("list", new KeyValue[] { new KeyValue("name", "default") }, (nodeArray.Length == 0) ? null : nodeArray);
            var node2 = new ProtocolTreeNode("query", new KeyValue[] { new KeyValue("xmlns", "jabber:iq:privacy") }, child);
            var node3 = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "set") }, node2);
            this.SendNode(node3);
        }

        public void SendStatusUpdate(string status)
        {
            string id = TicketManager.GenerateId();
            FMessage message = new FMessage(new FMessage.FMessageIdentifierKey("s.us", true, id));
            var messageNode = GetMessageNode(message, new ProtocolTreeNode("body", null, WhatsApp.SYSEncoding.GetBytes(status)));
            this.SendNode(messageNode);
        }

        public void SendSubjectReceived(string to, string id)
        {
            var child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            var node = GetSubjectMessage(to, id, child);
            this.SendNode(node);
        }

        public void SendUnsubscribeHim(string jid)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unsubscribed"), new KeyValue("to", jid) });
            this.SendNode(node);
        }

        public void SendUnsubscribeMe(string jid)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unsubscribe"), new KeyValue("to", jid) });
            this.SendNode(node);
        }

        public void SendVisibleReceiptAck(string to, string id)
        {
            this.SendReceiptAck(to, id, "visible");
        }

        internal void SendGetGroups(string id, string type)
        {
            var child = new ProtocolTreeNode("list", new[] { new KeyValue("type", type) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g"), new KeyValue("to", "g.us") }, child);
            this.SendNode(node);
        }

        internal void SendMessageWithBody(FMessage message, bool hidden = false)
        {
            var child = new ProtocolTreeNode("body", null, null, WhatsApp.SYSEncoding.GetBytes(message.data));
            this.SendNode(GetMessageNode(message, child, hidden));
        }

        internal void SendMessageWithMedia(FMessage message)
        {
            ProtocolTreeNode node;
            if (FMessage.Type.System == message.media_wa_type)
            {
                throw new SystemException("Cannot send system message over the network");
            }

            List<KeyValue> list = new List<KeyValue>(new KeyValue[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:mms"), new KeyValue("type", FMessage.GetMessage_WA_Type_StrValue(message.media_wa_type)) });
            if (FMessage.Type.Location == message.media_wa_type)
            {
                list.AddRange(new KeyValue[] { new KeyValue("latitude", message.latitude.ToString(CultureInfo.InvariantCulture)), new KeyValue("longitude", message.longitude.ToString(CultureInfo.InvariantCulture)) });
                if (message.location_details != null)
                {
                    list.Add(new KeyValue("name", message.location_details));
                }
                if (message.location_url != null)
                {
                    list.Add(new KeyValue("url", message.location_url));
                }
            }
            else if (((FMessage.Type.Contact != message.media_wa_type) && (message.media_name != null)) && ((message.media_url != null) && (message.media_size > 0L)))
            {
                list.AddRange(new KeyValue[] { new KeyValue("file", message.media_name), new KeyValue("size", message.media_size.ToString(CultureInfo.InvariantCulture)), new KeyValue("url", message.media_url) });
                if (message.media_duration_seconds > 0)
                {
                    list.Add(new KeyValue("seconds", message.media_duration_seconds.ToString(CultureInfo.InvariantCulture)));
                }
            }
            if ((FMessage.Type.Contact == message.media_wa_type) && (message.media_name != null))
            {
                node = new ProtocolTreeNode("media", list.ToArray(), new ProtocolTreeNode("vcard", new KeyValue[] { new KeyValue("name", message.media_name) }, WhatsApp.SYSEncoding.GetBytes(message.data)));
            }
            else
            {
                byte[] data = message.binary_data;
                if ((data == null) && !string.IsNullOrEmpty(message.data))
                {
                    try
                    {
                        data = Convert.FromBase64String(message.data);
                    }
                    catch (Exception)
                    {
                    }
                }
                if (data != null)
                {
                    list.Add(new KeyValue("encoding", "raw"));
                }
                node = new ProtocolTreeNode("media", list.ToArray(), null, data);
            }
            this.SendNode(GetMessageNode(message, node));
        }

        internal void SendVerbParticipants(string gjid, IEnumerable<string> participants, string id, string inner_tag)
        {
            IEnumerable<ProtocolTreeNode> source = from jid in participants select new ProtocolTreeNode("participant", new[] { new KeyValue("jid", jid) });
            var child = new ProtocolTreeNode(inner_tag, null, source);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", gjid) }, child);
            this.SendNode(node);
        }

        public void SendNode(ProtocolTreeNode node)
        {
            this.SendData(this.BinWriter.Write(node));
        }

        private IEnumerable<ProtocolTreeNode> ProcessGroupSettings(IEnumerable<GroupSetting> groups)
        {
            ProtocolTreeNode[] nodeArray = null;
            if ((groups != null) && groups.Any<GroupSetting>())
            {
                DateTime now = DateTime.Now;
                nodeArray = (from @group in groups
                             select new ProtocolTreeNode("item", new[] 
                { new KeyValue("jid", @group.Jid),
                    new KeyValue("notify", @group.Enabled ? "1" : "0"),
                    new KeyValue("mute", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", new object[] { (!@group.MuteExpiry.HasValue || (@group.MuteExpiry.Value <= now)) ? 0 : ((int) (@group.MuteExpiry.Value - now).TotalSeconds) })) })).ToArray<ProtocolTreeNode>();
            }
            return nodeArray;
        }

        private void SendReceiptAck(string to, string id, string receiptType)
        {
            var tmpChild = new ProtocolTreeNode("ack", new[] { new KeyValue("xmlns", "urn:xmpp:receipts"), new KeyValue("type", receiptType) });
            var resultNode = new ProtocolTreeNode("message", new[]
                                                             {
                                                                 new KeyValue("to", to),
                                                                 new KeyValue("type", "chat"),
                                                                 new KeyValue("id", id)
                                                             }, tmpChild);
            this.SendNode(resultNode);
        }

        internal static ProtocolTreeNode GetMessageNode(FMessage message, ProtocolTreeNode pNode, bool hidden = false)
        {
            return new ProtocolTreeNode("message", new[] { 
                new KeyValue("to", message.identifier_key.remote_jid), 
                new KeyValue("type", message.media_wa_type == FMessage.Type.Undefined?"text":"media"), 
                new KeyValue("id", message.identifier_key.id) 
            },
            new ProtocolTreeNode[] {
                new ProtocolTreeNode("x", new KeyValue[] { new KeyValue("xmlns", "jabber:x:event") }, new ProtocolTreeNode("server", null)),
                pNode,
                new ProtocolTreeNode("offline", null)
            });
        }

        public static ProtocolTreeNode GetSubjectMessage(string to, string id, ProtocolTreeNode child)
        {
            return new ProtocolTreeNode("message", new[] { new KeyValue("to", to), new KeyValue("type", "subject"), new KeyValue("id", id) }, child);
        }
    }
}
