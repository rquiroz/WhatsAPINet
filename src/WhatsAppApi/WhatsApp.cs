using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;
using WhatsAppApi.Settings;

namespace WhatsAppApi
{
    public class WhatsApp
    {
        private readonly Encoding sysEncoding;
        private AccountInfo accountinfo;
        private Dictionary<string, string> challengeArray;
        private string connectedStatus = "connected";
        private bool debug;
        private string disconnectedStatus = "disconnected";
        private string imei;
        private string loginStatus;
        private object messageLock = new object();
        private List<ProtocolTreeNode> messageQueue;
        private string name;
        private string phoneNumber;
        private BinTreeNodeReader reader;
        private BinTreeNodeWriter writer;
        private int timeout = 2000;
        
        private WhatsNetwork whatsNetwork;
        public WhatsSendHandler WhatsSendHandler { get; private set; }
        public WhatsParser WhatsParser { get; private set; }

        //array("sec" => 2, "usec" => 0);
        public WhatsApp(string phoneNum, string imei, string nick, bool debug = false)
        {
            this.messageQueue = new List<ProtocolTreeNode>();
            this.sysEncoding = Encoding.GetEncoding("ISO-8859-1");
            this.challengeArray = new Dictionary<string, string>();

            this.phoneNumber = phoneNum;
            this.imei = imei;
            this.name = nick;
            this.debug = debug;
            string[] dict = DecodeHelper.getDictionary();
            this.writer = new BinTreeNodeWriter(dict);
            this.reader = new BinTreeNodeReader(dict);

            this.loginStatus = disconnectedStatus;

            this.whatsNetwork = new WhatsNetwork(WhatsConstants.WhatsAppHost, WhatsConstants.WhatsPort, this.sysEncoding, this.timeout);
            this.WhatsParser = new WhatsParser(this.whatsNetwork);
            this.WhatsSendHandler = this.WhatsParser.WhatsSendHandler;
        }

        public void AddMessage(ProtocolTreeNode node)
        {
            lock (messageLock)
            {
                this.messageQueue.Add(node);
            }

        }

        public void Connect()
        {
            this.whatsNetwork.Connect();
        }

        public string encryptPassword()
        {
            if (this.imei.Contains(":"))
            {
                this.imei = this.imei.ToUpper();
                return md5(this.imei + this.imei);
            }
            else
            {
                return md5(new string(this.imei.Reverse().ToArray()));
            }
        }

        public AccountInfo GetAccountInfo()
        {
            return this.accountinfo;
        }

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

        public bool HasMessages()
        {
            if (this.messageQueue == null)
                return false;
            return this.messageQueue.Count > 0;
        }

        public void Login()
        {
            //"$this->device-$this->whatsAppVer-$this->port";
            string resource = string.Format(@"{0}-{1}-{2}",
                WhatsConstants.IphoneDevice,
                WhatsConstants.WhatsAppVer,
                WhatsConstants.WhatsPort);
            var data = this.writer.StartStream(WhatsConstants.WhatsAppServer, resource);
            var feat = this.addFeatures();
            var auth = this.addAuth();
            this.whatsNetwork.SendData(data);
            this.whatsNetwork.SendNode(feat);
            this.whatsNetwork.SendNode(auth);

            this.PollMessages();
            ProtocolTreeNode authResp = this.addAuthResponse();
            this.whatsNetwork.SendNode(authResp);
            int cnt = 0;
            do
            {
                this.PollMessages();
            } while ((cnt++ < 100) &&
                     (this.loginStatus.Equals(this.disconnectedStatus, StringComparison.OrdinalIgnoreCase)));
        }

        public void Message(string to, string txt)
        {
            //var bodyNode = new ProtocolTreeNode("body", null, txt);
            var tmpMessage = new FMessage(to, true)
                                 {key = {id = TicketCounter.MakeId("mSend_")}, data = txt};
            this.WhatsParser.WhatsSendHandler.SendMessage(tmpMessage);
        }

        public void MessageImage(string msgid, string to, string url, string file, string size, string icon)
        {
            //var mediaAttribs = new KeyValue[]
            //                       {
            //                           new KeyValue("xmlns", "urn:xmpp:whatsapp:mms"),
            //                           new KeyValue("type", "image"),
            //                           new KeyValue("url", url),
            //                           new KeyValue("file", file),
            //                           new KeyValue("size", size)
            //                       };

            //var mediaNode = new ProtocolTreeNode("media", mediaAttribs, icon);
            //this.SendMessageNode(msgid, to, mediaNode);
        }

        public void PollMessages()
        {
            this.processInboundData(this.whatsNetwork.ReadData());
        }

        public void Pong(string msgid)
        {
            this.WhatsParser.WhatsSendHandler.SendPong(msgid);

            //string whatsAppServer = this.whatsAppServer;

            //var messageHash = new KeyValue[]
            //                      {
            //                          new KeyValue("to", whatsAppServer), 
            //                          new KeyValue("id", msgid), 
            //                          new KeyValue("type", "result"), 
            //                      };

            //var messsageNode = new ProtocolTreeNode("iq", messageHash, null);
            //this.whatsNetwork.SendNode(messsageNode);
        }

        public void RequestLastSeen(string jid)
        {
            this.WhatsParser.WhatsSendHandler.SendQueryLastOnline(jid);
        }

        public void sendNickname(string nickname)
        {
            this.WhatsParser.WhatsSendHandler.SendAvailableForChat(nickname); //es muss der nickname festgelegt werden
        }

        protected ProtocolTreeNode addAuth()
        {
            var node = new ProtocolTreeNode("auth",
                new KeyValue[] { new KeyValue("xmlns", @"urn:ietf:params:xml:ns:xmpp-sasl"), new KeyValue("mechanism", "DIGEST-MD5-1") });
            return node;
        }

        protected ProtocolTreeNode addAuthResponse()
        {
            if (!this.challengeArray.ContainsKey("nonce"))
                this.challengeArray.Add("nonce", "");

            string resp = this.authenticate(this.challengeArray["nonce"]);

            var node = new ProtocolTreeNode("response",
                new KeyValue[] { new KeyValue("xmlns", "urn:ietf:params:xml:ns:xmpp-sasl") },
                Func.EncodeTo64(resp, this.sysEncoding));
            return node;
        }

        protected ProtocolTreeNode addFeatures()
        {
            var child = new ProtocolTreeNode("receipt_acks", null);
            var childList = new List<ProtocolTreeNode>();
            childList.Add(child);
            var parent = new ProtocolTreeNode("stream:features", null, childList, "");
            return parent;
        }
        protected string authenticate(string nonce)
        {
            string NC = "00000001";
            string qop = "auth";
            string cnonce = Func.random_uuid();
            string data1 = this.phoneNumber;
            data1 += ":";
            data1 += WhatsConstants.WhatsAppServer;
            data1 += ":";
            data1 += this.encryptPassword(); //this.EncryptPassword();

            string data2 = Func.HexString2Ascii(md5(data1));
            data2 += ":";
            data2 += nonce;
            data2 += ":";
            data2 += cnonce;

            string data3 = "AUTHENTICATE:";
            data3 += WhatsConstants.WhatsAppDigest;

            string data4 = md5(data2);
            data4 += ":";
            data4 += nonce;
            data4 += ":";
            data4 += NC;
            data4 += ":";
            data4 += cnonce;
            data4 += ":";
            data4 += qop;
            data4 += ":";
            data4 += md5(data3);

            string data5 = md5(data4);
            string response =
                string.Format(
                    "username=\"{0}\",realm=\"{1}\",nonce=\"{2}\",cnonce=\"{3}\",nc={4},qop={5},digest-uri=\"{6}\",response={7},charset=ISO-8859-1",
                    this.phoneNumber,
                    WhatsConstants.WhatsAppRealm,
                    nonce,
                    cnonce,
                    NC,
                    qop,
                    WhatsConstants.WhatsAppDigest,
                    data5);
            return response;
        }

        protected void DebugPrint(string debugMsg)
        {
            if (this.debug && debugMsg.Length > 0)
            {
                Console.WriteLine(debugMsg);
            }
        }

        protected void processChallenge(ProtocolTreeNode node)
        {
            string challenge = Func.DecodeTo64(node.data, this.sysEncoding);
            string[] challengeStrs = challenge.Split(',');
            this.challengeArray = new Dictionary<string, string>();
            foreach (var item in challengeStrs)
            {
                string[] d = item.Split('=');
                if (this.challengeArray.ContainsKey(d[0]))
                    this.challengeArray[d[0]] = d[1].Replace("\"", "");
                else
                    this.challengeArray.Add(d[0], d[1].Replace("\"", ""));
            }
        }

        protected void processInboundData(string data)
        {
            try
            {
                var node = this.reader.nextTree(data);
                while (node != null)
                {
                    this.WhatsParser.ParseProtocolNode(node);
                    this.DebugPrint(node.NodeString("RECVD: "));
                    if (node.tag.Equals("challenge", StringComparison.OrdinalIgnoreCase))
                    {
                        this.processChallenge(node);
                    }
                    else if (node.tag.Equals("success", StringComparison.OrdinalIgnoreCase))
                    {
                        this.loginStatus = this.connectedStatus;
                        this.accountinfo = new AccountInfo(node.GetAttribute("status"),
                                                           node.GetAttribute("kind"),
                                                           node.GetAttribute("creation"),
                                                           node.GetAttribute("expiration"));
                    }
                    if (node.tag.Equals("message", StringComparison.OrdinalIgnoreCase))
                    {
                        this.AddMessage(node);
                        this.sendMessageReceived(node);
                    }
                    if (node.tag.Equals("iq", StringComparison.OrdinalIgnoreCase)
                        && node.GetAttribute("type").Equals("get", StringComparison.OrdinalIgnoreCase)
                        && node.children.First().tag.Equals("ping", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Pong(node.GetAttribute("id"));
                    }
                    node = this.reader.nextTree();
                }
            }
            catch (IncompleteMessageException e)
            {
            }
        }

        protected void sendMessageReceived(ProtocolTreeNode msg)
        {
            //this.WhatsParser.WhatsSendHandler.SendMessageReceived();
            ProtocolTreeNode requestNode = msg.GetChild("request");
            if (requestNode == null ||
                !requestNode.GetAttribute("xmlns").Equals("urn:xmpp:receipts", StringComparison.OrdinalIgnoreCase))
                return;

            FMessage tmpMessage = new FMessage(new FMessage.Key(msg.GetAttribute("from"), true, msg.GetAttribute("id")));
            this.WhatsParser.WhatsSendHandler.SendMessageReceived(tmpMessage);
            //var receivedNode = new ProtocolTreeNode("received",
            //                                        new[] {new KeyValue("xmlns", "urn:xmpp:receipts")});

            //var messageNode = new ProtocolTreeNode("message",
            //                                       new[]
            //                                           {
            //                                               new KeyValue("to", msg.GetAttribute("from")),
            //                                               new KeyValue("type", "chat"),
            //                                               new KeyValue("id", msg.GetAttribute("id"))
            //                                           },
            //                                       new[] {receivedNode});
            //this.whatsNetwork.SendNode(messageNode);
        }

        private string md5(string pass)
        {
            MD5 md5 = MD5.Create();
            byte[] dataMd5 = md5.ComputeHash(this.sysEncoding.GetBytes(pass));
            var sb = new StringBuilder();
            for (int i = 0; i < dataMd5.Length; i++)
                sb.AppendFormat("{0:x2}", dataMd5[i]);
            return sb.ToString();
        }
        private void PrintInfo(string p)
        {
            this.DebugPrint(p);
        }

        /**
         * TODO
         */
    }
}
