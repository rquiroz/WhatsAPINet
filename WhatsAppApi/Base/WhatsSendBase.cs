﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;
using WhatsAppApi.Settings;

namespace WhatsAppApi
{
    public class WhatsSendBase : WhatsAppBase
    {
        protected bool m_usePoolMessages = false;

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

            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            TicketCounter.setLoginTime(unixTimestamp.ToString());
            this.SendAvailableForChat(this.name, this.hidden);
        }

        public void PollMessages(bool autoReceipt = true)
        {
            m_usePoolMessages = true;
            while (pollMessage(autoReceipt)) ;
            m_usePoolMessages = false;
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

        protected ProtocolTreeNode addFeatures()
        {
            ProtocolTreeNode readReceipts = new ProtocolTreeNode("readreceipts", null, null, null);
            ProtocolTreeNode groups_v2 = new ProtocolTreeNode("groups_v2", null, null, null);
            ProtocolTreeNode privacy = new ProtocolTreeNode("privacy", null, null, null);
            ProtocolTreeNode presencev2 = new ProtocolTreeNode("presence", null, null, null);
            return new ProtocolTreeNode("stream:features", null, new ProtocolTreeNode[] { readReceipts, groups_v2, privacy, presencev2 }, null);
        }

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
                data = b.ToArray();

                this._challengeBytes = null;

                this.outputKey.EncodeMessage(data, 0, 4, data.Length - 4);

                this.BinWriter.Key = this.outputKey;
            }

            return data;
        }

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
                var node = new ProtocolTreeNode("response", null, null, data);

                return node;
            }
            throw new Exception("Auth response error");
        }

        protected void processChallenge(ProtocolTreeNode node)
        {
            _challengeBytes = node.data;
        }

        protected bool processInboundData(byte[] msgdata, bool autoReceipt = true)
        {
            try
            {
                ProtocolTreeNode node = this.reader.nextTree(msgdata);
                if (node != null)
                {
                    //foreach ( ProtocolTreeNode x in node.GetAllChildren() )
                    //{
                    //    Console.Write(x.GetData().ToString());
                    //}

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
                        this.fireOnLoginSuccess(this.phoneNumber, node.GetData());
                    }
                    else if (ProtocolTreeNode.TagEquals(node, "failure"))
                    {
                        this.loginStatus = CONNECTION_STATUS.UNAUTHORIZED;
                        this.fireOnLoginFailed(node.children.FirstOrDefault().tag);
                    }

                    if (ProtocolTreeNode.TagEquals(node, "receipt"))
                    {
                        string from = node.GetAttribute("from");
                        string participant = node.GetAttribute("participant");
                        string id = node.GetAttribute("id");
                        string type = node.GetAttribute("type") ?? "delivery";
                        switch (type)
                        {
                            case "delivery":
                                //delivered to target
                                FireOnGetMessageReceivedClient(from, participant, id);
                                break;
                            case "read":
                                FireOnGetMessageReadClient(from, participant, id);
                                //todo
                                break;
                            case "played":
                                //played by target
                                //todo
                                break;
                        }

                        var list = node.GetChild("list");
                        if (list != null)
                            foreach (var receipt in list.GetAllChildren())
                            {
                                FireOnGetMessageReceivedClient(from, participant, receipt.GetAttribute("id"));
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
                            Helper.DebugAdapter.Instance.fireOnPrintDebug("Error : " + content);
                        }
                        this.Disconnect();
                    }

                    if (ProtocolTreeNode.TagEquals(node, "presence"))
                    {
                        //presence node
                        this.fireOnGetPresence(node.GetAttribute("from"), node.GetAttribute("type"));
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
                                this.fireOnGetTyping(node.GetAttribute("from"));
                                break;
                            case "paused":
                                this.fireOnGetPaused(node.GetAttribute("from"));
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
                            FireOnGetMessageReceivedServer(node.GetAttribute("from"), node.GetAttribute("participant"), node.GetAttribute("id"));
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
                this.fireOnGetContactName(node.GetAttribute("from"), name);
            }
            if (node.GetAttribute("type") == "error")
            {
                throw new NotImplementedException(node.NodeString());
            }
            if (node.GetChild("body") != null || node.GetChild("enc") != null)
            {
                // text message
                // encrypted messages have no body node. Instead, the encrypted cipher text is provided within the enc node
                var contentNode = node.GetChild("body") ?? node.GetChild("enc");
                if (contentNode != null)
                {
                    this.fireOnGetMessage(node, node.GetAttribute("from"), node.GetAttribute("id"),
                                        node.GetAttribute("notify"), System.Text.Encoding.UTF8.GetString(contentNode.GetData()),
                                        autoReceipt);
                    if (autoReceipt)
                    {
                        this.sendMessageReceived(node);
                    }
                }
            }
            if (node.GetChild("media") != null)
            {
                ProtocolTreeNode media = node.GetChild("media");
                //media message

                //define variables in switch
                string UserName;
                string file, url, from, id;
                int size;
                byte[] preview, dat;
                id = node.GetAttribute("id");
                from = node.GetAttribute("from");
                UserName = node.GetAttribute("notify");
                switch (media.GetAttribute("type"))
                {
                    case "image":
                        url = media.GetAttribute("url");
                        file = media.GetAttribute("file");
                        size = Int32.Parse(media.GetAttribute("size"));
                        preview = media.GetData();
                        this.fireOnGetMessageImage(node, from, id, file, size, url, preview, UserName);
                        break;
                    case "audio":
                        file = media.GetAttribute("file");
                        size = Int32.Parse(media.GetAttribute("size"));
                        url = media.GetAttribute("url");
                        preview = media.GetData();
                        this.fireOnGetMessageAudio(node, from, id, file, size, url, preview, UserName);
                        break;
                    case "video":
                        file = media.GetAttribute("file");
                        size = Int32.Parse(media.GetAttribute("size"));
                        url = media.GetAttribute("url");
                        preview = media.GetData();
                        this.fireOnGetMessageVideo(node, from, id, file, size, url, preview, UserName);
                        break;
                    case "location":
                        double lon = double.Parse(media.GetAttribute("longitude"), System.Globalization.CultureInfo.InvariantCulture);
                        double lat = double.Parse(media.GetAttribute("latitude"), System.Globalization.CultureInfo.InvariantCulture);
                        preview = media.GetData();
                        name = media.GetAttribute("name");
                        url = media.GetAttribute("url");
                        this.fireOnGetMessageLocation(node, from, id, lon, lat, url, name, preview, UserName);
                        break;
                    case "vcard":
                        ProtocolTreeNode vcard = media.GetChild("vcard");
                        name = vcard.GetAttribute("name");
                        dat = vcard.GetData();
                        this.fireOnGetMessageVcard(node, from, id, name, dat);
                        break;
                }
                this.sendMessageReceived(node);
            }
        }

        protected void handleIq(ProtocolTreeNode node)
        {
            if (node.GetAttribute("type") == "error")
            {
                this.fireOnError(node.GetAttribute("id"), node.GetAttribute("from"), Int32.Parse(node.GetChild("error").GetAttribute("code")), node.GetChild("error").GetAttribute("text"));
            }
            if (node.GetChild("sync") != null)
            {
                //sync result
                ProtocolTreeNode sync = node.GetChild("sync");
                ProtocolTreeNode existing = sync.GetChild("in");
                ProtocolTreeNode nonexisting = sync.GetChild("out");
                //process existing first
                Dictionary<string, string> existingUsers = new Dictionary<string, string>();
                if (existing != null)
                {
                    foreach (ProtocolTreeNode child in existing.GetAllChildren())
                    {
                        existingUsers.Add(System.Text.Encoding.UTF8.GetString(child.GetData()), child.GetAttribute("jid"));
                    }
                }
                //now process failed numbers
                List<string> failedNumbers = new List<string>();
                if (nonexisting != null)
                {
                    foreach (ProtocolTreeNode child in nonexisting.GetAllChildren())
                    {
                        failedNumbers.Add(System.Text.Encoding.UTF8.GetString(child.GetData()));
                    }
                }
                int index = 0;
                Int32.TryParse(sync.GetAttribute("index"), out index);
                this.fireOnGetSyncResult(index, sync.GetAttribute("sid"), existingUsers, failedNumbers.ToArray());
            }

            // last seen
            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("query") != null
            )
            {                
                DateTime lastSeen = DateTime.Now.AddSeconds(double.Parse(node.children.FirstOrDefault().GetAttribute("seconds")) * -1);
                this.fireOnGetLastSeen(node.GetAttribute("from"), lastSeen);
            }

            // media upload
            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && (node.GetChild("media") != null || node.GetChild("duplicate") != null)
                )
            {
                this.uploadResponse = node;
            }

            // profile picture
            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("picture") != null
                )
            {
                string from = node.GetAttribute("from");
                string id = node.GetChild("picture").GetAttribute("id");
                byte[] dat = node.GetChild("picture").GetData();
                string type = node.GetChild("picture").GetAttribute("type");
                if (type == "preview")
                {
                    this.fireOnGetPhotoPreview(from, id, dat);
                }
                else
                {
                    this.fireOnGetPhoto(from, id, dat);
                }
            }

            // ping
            if (node.GetAttribute("type").Equals("get", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("ping") != null)
            {
                this.SendPong(node.GetAttribute("id"));
            }

            // group(s) info
            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("group") != null)
            {
                List<WaGroupInfo> groups = new List<WaGroupInfo>();
                foreach (ProtocolTreeNode group in node.children)
                {
                    groups.Add(new WaGroupInfo(
                        group.GetAttribute("id"),
                        group.GetAttribute("owner"),
                        group.GetAttribute("creation"),
                        group.GetAttribute("subject"),
                        group.GetAttribute("s_t"),
                        group.GetAttribute("s_o")
                        ));
                }
                this.fireOnGetGroups(groups.ToArray());
            }

            // group participants
            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("participant") != null)
            {
                List<string> participants = new List<string>();
                foreach (ProtocolTreeNode part in node.GetAllChildren())
                {
                    if (part.tag == "participant" && !string.IsNullOrEmpty(part.GetAttribute("jid")))
                    {
                        participants.Add(part.GetAttribute("jid"));
                    }
                }
                this.fireOnGetGroupParticipants(node.GetAttribute("from"), participants.ToArray());
            }

            // status
            if (node.GetAttribute("type") == "result" && node.GetChild("status") != null)
            {
                foreach (ProtocolTreeNode status in node.GetChild("status").GetAllChildren())
                {
                    this.fireOnGetStatus(status.GetAttribute("jid"),
                        "result",
                        null,
                        WhatsApp.SYSEncoding.GetString(status.GetData()));
                }
            }

            // privacy
            if (node.GetAttribute("type") == "result" && node.GetChild("privacy") != null)
            {
                Dictionary<VisibilityCategory, VisibilitySetting> settings = new Dictionary<VisibilityCategory, VisibilitySetting>();
                foreach (ProtocolTreeNode child in node.GetChild("privacy").GetAllChildren("category"))
                {
                    settings.Add(this.parsePrivacyCategory(
                        child.GetAttribute("name")), 
                        this.parsePrivacySetting(child.GetAttribute("value"))
                    );
                }
                this.fireOnGetPrivacySettings(settings);
            }

            // broadcast lists
            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("list") != null
            )
            {
                var lists = new List<string>();
                foreach (var list in node.GetAllChildren("list"))
                {
                    lists.Add(list.GetAttribute("id"));
                }
                fireOnGetBroadcastLists(lists);
            }
        }

        protected void handleNotification(ProtocolTreeNode node)
        {
            if (!String.IsNullOrEmpty(node.GetAttribute("notify")))
            {
                this.fireOnGetContactName(node.GetAttribute("from"), node.GetAttribute("notify"));
            }
            string type = node.GetAttribute("type");
            switch (type)
            {
                case "picture":
                    ProtocolTreeNode child = node.children.FirstOrDefault();
                    this.fireOnNotificationPicture(child.tag, 
                        child.GetAttribute("jid"), 
                        child.GetAttribute("id"));
                    break;
                case "status":
                    ProtocolTreeNode child2 = node.children.FirstOrDefault();
                    this.fireOnGetStatus(node.GetAttribute("from"), 
                        child2.tag, 
                        node.GetAttribute("notify"), 
                        System.Text.Encoding.UTF8.GetString(child2.GetData()));
                    break;
                case "subject":
                    //fire username notify
                    this.fireOnGetContactName(node.GetAttribute("participant"),
                        node.GetAttribute("notify"));
                    //fire subject notify
                    this.fireOnGetGroupSubject(node.GetAttribute("from"),
                        node.GetAttribute("participant"),
                        node.GetAttribute("notify"),
                        System.Text.Encoding.UTF8.GetString(node.GetChild("body").GetData()),
                        GetDateTimeFromTimestamp(node.GetAttribute("t")));
                    break;
                case "contacts":
                    //TODO
                    break;
                case "participant":
                    string gjid = node.GetAttribute("from");
                    string t = node.GetAttribute("t");
                    foreach (ProtocolTreeNode child3 in node.GetAllChildren())
                    {
                        if (child3.tag == "add")
                        {
                            this.fireOnGetParticipantAdded(gjid, 
                                child3.GetAttribute("jid"), 
                                GetDateTimeFromTimestamp(t));
                        }
                        else if (child3.tag == "remove")
                        {
                            this.fireOnGetParticipantRemoved(gjid, 
                                child3.GetAttribute("jid"), 
                                child3.GetAttribute("author"), 
                                GetDateTimeFromTimestamp(t));
                        }
                        else if (child3.tag == "modify")
                        {
                            this.fireOnGetParticipantRenamed(gjid,
                                child3.GetAttribute("remove"),
                                child3.GetAttribute("add"),
                                GetDateTimeFromTimestamp(t));
                        }
                    }
                    break;
            }
            this.SendNotificationAck(node);
        }

        private void SendNotificationAck(ProtocolTreeNode node, string type = null)
        {
            string from = node.GetAttribute("from");
            string to = node.GetAttribute("to");
            string participant = node.GetAttribute("participant");
            string id = node.GetAttribute("id");

            List<KeyValue> attributes = new List<KeyValue>();
            if (!string.IsNullOrEmpty(to))
            {
                attributes.Add(new KeyValue("from", to));
            }
            if (!string.IsNullOrEmpty(participant))
            {
                attributes.Add(new KeyValue("participant", participant));
            }

            if (!string.IsNullOrEmpty(type))
            {
                attributes.Add(new KeyValue("type", type));
            }
            attributes.AddRange(new[] {
                new KeyValue("to", from),
                new KeyValue("class", node.tag),
                new KeyValue("id", id)
             });

            ProtocolTreeNode sendNode = new ProtocolTreeNode("ack", attributes.ToArray());
            this.SendNode(sendNode);
        }

        protected void sendMessageReceived(ProtocolTreeNode msg, string type = "read")
        {
            FMessage tmpMessage = new FMessage(new FMessage.FMessageIdentifierKey(msg.GetAttribute("from"), true, msg.GetAttribute("id")));
            this.SendMessageReceived(tmpMessage, type);
        }

        public void SendAvailableForChat(string nickName = null, bool isHidden = false)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("name", (!String.IsNullOrEmpty(nickName) ? nickName : this.name)) });
            this.SendNode(node);
        }

        protected void SendClearDirty(IEnumerable<string> categoryNames)
        {
            string id = TicketCounter.MakeId();
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

        protected void SendClearDirty(string category)
        {
            this.SendClearDirty(new string[] { category });
        }

        protected void SendDeliveredReceiptAck(string to, string id)
        {
            this.SendReceiptAck(to, id, "delivered");
        }

        protected void SendMessageReceived(FMessage message, string type = "read")
        {

            KeyValue toAttrib = new KeyValue("to", message.identifier_key.remote_jid);
            KeyValue idAttrib = new KeyValue("id", message.identifier_key.id);

            var attribs = new List<KeyValue>();
            attribs.Add(toAttrib);
            attribs.Add(idAttrib);
            if (type.Equals("read"))
            {
                KeyValue typeAttrib = new KeyValue("type", type);
                attribs.Add(typeAttrib);
            }

            ProtocolTreeNode node = new ProtocolTreeNode("receipt", attribs.ToArray());

            this.SendNode(node);
        }

        protected void SendNotificationReceived(string jid, string id)
        {
            var child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            var node = new ProtocolTreeNode("message", new[] { new KeyValue("to", jid), new KeyValue("type", "notification"), new KeyValue("id", id) }, child);
            this.SendNode(node);
        }

        protected void SendPong(string id)
        {
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("type", "result"), new KeyValue("to", WhatsConstants.WhatsAppRealm), new KeyValue("id", id) });
            this.SendNode(node);
        }

        private void SendReceiptAck(string to, string id, string receiptType)
        {
            var tmpChild = new ProtocolTreeNode("ack", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            var resultNode = new ProtocolTreeNode("message", new[]
                                                             {
                                                                 new KeyValue("to", to),
                                                                 new KeyValue("type", "chat"),
                                                                 new KeyValue("id", id)
                                                             }, tmpChild);
            this.SendNode(resultNode);
        }
    }
}
