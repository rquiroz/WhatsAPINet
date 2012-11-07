using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WhatsAppApi.Helper;
using WhatsAppApi.Settings;

namespace WhatsAppApi.Response
{
    public class WhatsParser
    {
        public WhatsSendHandler WhatsSendHandler { get; private set; }
        private WhatsNetwork whatsNetwork;
        private MessageRecvResponse messResponseHandler;
        private BinTreeNodeWriter _binWriter;

        internal WhatsParser(WhatsNetwork whatsNet, BinTreeNodeWriter writer)
        {
            this.WhatsSendHandler = new WhatsSendHandler(whatsNet, writer);
            this.whatsNetwork = whatsNet;
            this.messResponseHandler = new MessageRecvResponse(this.WhatsSendHandler);
            this._binWriter = writer;
        }

        public void ParseProtocolNode(ProtocolTreeNode protNode)
        {
            if (ProtocolTreeNode.TagEquals(protNode, "iq"))
            {
                string attributeValue = protNode.GetAttribute("type");
                string id = protNode.GetAttribute("id");
                string str3 = protNode.GetAttribute("from");
                if (attributeValue == null)
                {
                    throw new Exception("Message-Corrupt: missing 'type' attribute in iq stanza");
                }
                if (!attributeValue.Equals("result"))
                {
                    if (attributeValue.Equals("error"))
                    {
                        //IqResultHandler handler2 = this.PopIqHandler(id);
                        //if (handler2 != null)
                        //{
                        //    handler2.ErrorNode(node);
                        //}
                    }
                    else if (!attributeValue.Equals("get"))
                    {
                        if (!attributeValue.Equals("set"))
                        {
                            throw new Exception("Message-Corrupt: unknown iq type attribute: " + attributeValue);
                        }
                        ProtocolTreeNode child = protNode.GetChild("query");
                        if (child != null)
                        {
                            string str8 = child.GetAttribute("xmlns");
                            if ("jabber:iq:roster" == str8)
                            {
                                foreach (ProtocolTreeNode node5 in child.GetAllChildren("item"))
                                {
                                    node5.GetAttribute("jid");
                                    node5.GetAttribute("subscription");
                                    node5.GetAttribute("ask");
                                }
                            }
                        }
                    }
                    else
                    {
                        ProtocolTreeNode node3 = protNode.GetChild("ping");
                        if (node3 != null)
                        {
                            //this.EventHandler.OnPing(id);
                        }
                        else if ((!ProtocolTreeNode.TagEquals(node3, "query")
                                  || (str3 == null))
                                 && (ProtocolTreeNode.TagEquals(node3, "relay")
                                     && (str3 != null)))
                        {
                            int num;
                            string pin = node3.GetAttribute("pin");
                            string tmpTimeout = node3.GetAttribute("timeout");
                            if (
                                !int.TryParse(tmpTimeout ?? "0", WhatsConstants.WhatsAppNumberStyle, CultureInfo.InvariantCulture,
                                              out num))
                            {
                                //throw new CorruptStreamException(
                                //    "relay-iq exception parsing timeout attribute: " + tmpTimeout);
                            }
                            if (pin != null)
                            {
                                //this.EventHandler.OnRelayRequest(pin, num, id);
                            }
                        }
                    }
                }
                else
                {
                    //IqResultHandler handler = this.PopIqHandler(id);
                    //if (handler != null)
                    //{
                    //    handler.Parse(node, str3);
                    //}
                    //else if (id.StartsWith(this.Login.User))
                    //{
                    //    ProtocolNode node2 = node.GetChild(0);
                    //    ProtocolNode.Require(node2, "account");
                    //    string str4 = node2.GetAttribute("kind");
                    //    if ("paid".Equals(str4))
                    //    {
                    //        this.account_kind = AccountKind.Paid;
                    //    }
                    //    else if ("free".Equals(str4))
                    //    {
                    //        this.account_kind = AccountKind.Free;
                    //    }
                    //    else
                    //    {
                    //        this.account_kind = AccountKind.Unknown;
                    //    }
                    //    string s = node2.GetAttribute("expiration");
                    //    if (s == null)
                    //    {
                    //        throw new IOException("no expiration");
                    //    }
                    //    try
                    //    {
                    //        this.expire_date = long.Parse(s, CultureInfo.InvariantCulture);
                    //    }
                    //    catch (FormatException)
                    //    {
                    //        throw new IOException("invalid expire date: " + s);
                    //    }
                    //    this.EventHandler.OnAccountChange(this.account_kind, this.expire_date);
                    //}
                }
            }
            else if (ProtocolTreeNode.TagEquals(protNode, "presence"))
            {
                string str9 = protNode.GetAttribute("xmlns");
                string jid = protNode.GetAttribute("from");
                if (((str9 == null) || "urn:xmpp".Equals(str9)) && (jid != null))
                {
                    string str11 = protNode.GetAttribute("type");
                    if ("unavailable".Equals(str11))
                    {
                        //this.EventHandler.OnAvailable(jid, false);
                    }
                    else if ((str11 == null) || "available".Equals(str11))
                    {
                        //this.EventHandler.OnAvailable(jid, true);
                    }
                }
                else if ("w".Equals(str9) && (jid != null))
                {
                    string str12 = protNode.GetAttribute("add");
                    string str13 = protNode.GetAttribute("remove");
                    string str14 = protNode.GetAttribute("status");
                    if (str12 == null)
                    {
                        if (str13 == null)
                        {
                            if ("dirty".Equals(str14))
                            {
                                Dictionary<string, long> categories = ParseCategories(protNode);
                                //this.EventHandler.OnDirty(categories);
                            }
                        }
                        //else if (this.GroupEventHandler != null)
                        //{
                        //    this.GroupEventHandler.OnGroupRemoveUser(jid, str13);
                        //}
                    }
                    //else if (this.GroupEventHandler != null)
                    //{
                    //    this.GroupEventHandler.OnGroupAddUser(jid, str12);
                    //}
                }
            }
            else if (ProtocolTreeNode.TagEquals(protNode, "message"))
            {
                this.messResponseHandler.ParseMessageRecv(protNode);
            }
            else if ((ProtocolTreeNode.TagEquals(protNode, "ib") /*&& (this.EventHandler != null)*/) &&
                     (protNode.GetChild("offline") != null))
            {
                //this.EventHandler.OnOfflineMessagesCompleted();
            }
        }


        //internal void ParseMessageInitialTagAlreadyChecked(ProtocolTreeNode messageNode)
        //{
        //    FMessage.Builder builder = new FMessage.Builder();
        //    string tmpAttrbId = messageNode.GetAttribute("id");
        //    string tmpNodeFrom = messageNode.GetAttribute("from");
        //    string ujid = messageNode.GetAttribute("author") ?? "";
        //    string tmpNodeType = messageNode.GetAttribute("type");
        //    string tmpNodeAttrib = messageNode.GetAttribute("t");
        //    long result = 0L;
        //    if (!string.IsNullOrEmpty(tmpNodeAttrib) && long.TryParse(tmpNodeAttrib, out result))
        //    {
        //        builder.Timestamp(new DateTime?(WhatsConstants.UnixEpoch.AddSeconds((double)result)));
        //    }
        //    if ("error".Equals(tmpNodeType))
        //    {
        //        int num2 = 0;
        //        foreach (ProtocolTreeNode node in messageNode.GetAllChildren("error"))
        //        {
        //            string tmpCode = node.GetAttribute("code");
        //            try
        //            {
        //                num2 = int.Parse(tmpCode, CultureInfo.InvariantCulture);
        //            }
        //            catch (Exception)
        //            {
        //            }
        //        }
        //        if ((tmpNodeFrom != null) && (tmpAttrbId != null))
        //        {
        //            FMessage.Key key = new FMessage.Key(tmpNodeFrom, true, tmpAttrbId);
        //            //ErrorHandler handler = null;
        //            //if (trackedMessages.TryGetValue(key, out handler))
        //            //{
        //            //    trackedMessages.Remove(key);
        //            //    handler.OnError(num2);
        //            //}
        //            //else
        //            //{
        //            //    this.EventHandler.OnMessageError(key, num2);
        //            //}
        //        }
        //    }
        //    else if ("subject".Equals(tmpNodeType))
        //    {
        //        bool flag = false;
        //        foreach (ProtocolTreeNode node2 in messageNode.GetAllChildren("request"))
        //        {
        //            if ("urn:xmpp:receipts".Equals(node2.GetAttribute("xmlns")))
        //            {
        //                flag = true;
        //            }
        //        }
        //        ProtocolTreeNode child = messageNode.GetChild("body");
        //        string subject = (child == null) ? null : child.GetDataString();
        //        //if ((subject != null) && (this.GroupEventHandler != null))
        //        //{
        //        //    this.GroupEventHandler.OnGroupNewSubject(str3, ujid, subject, int.Parse(s, CultureInfo.InvariantCulture));
        //        //}
        //        //if (flag)
        //        //{
        //        //    this.SendSubjectReceived(str3, attributeValue);
        //        //}
        //    }
        //    else if ("chat".Equals(tmpNodeType))
        //    {
        //        bool duplicate = false;
        //        foreach (ProtocolTreeNode itemNode in (messageNode.GetAllChildren() ?? new ProtocolTreeNode[0]))
        //        {
        //            if (ProtocolTreeNode.TagEquals(itemNode, "composing"))
        //            {
        //                //if (this.EventHandler != null)
        //                //{
        //                //    this.EventHandler.OnIsTyping(str3, true);
        //                //}
        //            }
        //            else if (ProtocolTreeNode.TagEquals(itemNode, "paused"))
        //            {
        //                //if (this.EventHandler != null)
        //                //{
        //                //    this.EventHandler.OnIsTyping(str3, false);
        //                //}
        //            }
        //            else if (ProtocolTreeNode.TagEquals(itemNode, "body") && (tmpAttrbId != null))
        //            {
        //                string dataString = itemNode.GetDataString();
        //                FMessage.Key key2 = new FMessage.Key(tmpNodeFrom, false, tmpAttrbId);
        //                builder.Key(key2).Remote_resource(ujid).NewIncomingInstance().Data(dataString);
        //            }
        //            else if (ProtocolTreeNode.TagEquals(itemNode, "media") && (tmpAttrbId != null))
        //            {
        //                long num3;
        //                int num4;
        //                builder.Media_wa_type(FMessage.GetMessage_WA_Type(itemNode.GetAttribute("type"))).Media_url(itemNode.GetAttribute("url")).Media_name(itemNode.GetAttribute("file"));
        //                if (long.TryParse(itemNode.GetAttribute("size"), WhatsConstants.WhatsAppNumberStyle, CultureInfo.InvariantCulture, out num3))
        //                {
        //                    builder.Media_size(num3);
        //                }
        //                string str10 = itemNode.GetAttribute("seconds");
        //                if ((str10 != null) && int.TryParse(str10, WhatsConstants.WhatsAppNumberStyle, CultureInfo.InvariantCulture, out num4))
        //                {
        //                    builder.Media_duration_seconds(num4);
        //                }
        //                if (((FMessage.Type)builder.Media_wa_type().Value) == FMessage.Type.Location)
        //                {
        //                    double num5 = 0;
        //                    double num6 = 0;
        //                    string str11 = itemNode.GetAttribute("latitude");
        //                    string str12 = itemNode.GetAttribute("longitude");
        //                    if ((str11 == null) || (str12 == null))
        //                    {
        //                        str11 = "0";
        //                        str12 = "0";
        //                    }
        //                    if (!double.TryParse(str11, WhatsConstants.WhatsAppNumberStyle, CultureInfo.InvariantCulture, out num5) || !double.TryParse(str12, WhatsConstants.WhatsAppNumberStyle, CultureInfo.InvariantCulture, out num6))
        //                    {
        //                        //throw new CorruptStreamException("location message exception parsing lat or long attribute: " + str11 + " " + str12);
        //                    }
        //                    builder.Latitude(num5).Longitude(num6);
        //                    string details = itemNode.GetAttribute("name");
        //                    string url = itemNode.GetAttribute("url");
        //                    if (details != null)
        //                    {
        //                        builder.Location_details(details);
        //                    }
        //                    if (url != null)
        //                    {
        //                        builder.Location_url(url);
        //                    }
        //                }
        //                if (((FMessage.Type)builder.Media_wa_type().Value) == FMessage.Type.Contact)
        //                {
        //                    ProtocolTreeNode node5 = itemNode.GetChild("media");
        //                    if (node5 != null)
        //                    {
        //                        builder.Media_name(node5.GetAttribute("name")).Data(node5.GetDataString());
        //                    }
        //                }
        //                else
        //                {
        //                    string str15 = itemNode.GetAttribute("encoding") ?? "text";
        //                    if (str15 == "text")
        //                    {
        //                        builder.Data(itemNode.GetDataString());
        //                    }
        //                    else
        //                    {
        //                        //builder.BinaryData(messageNode.data);
        //                    }
        //                }
        //                FMessage.Key key3 = new FMessage.Key(tmpNodeFrom, false, tmpAttrbId);
        //                builder.Key(key3).Remote_resource(ujid).NewIncomingInstance();
        //            }
        //            else if (ProtocolTreeNode.TagEquals(itemNode, "request"))
        //            {
        //                builder.Wants_receipt(true);
        //            }
        //            else if (ProtocolTreeNode.TagEquals(itemNode, "x"))
        //            {
        //                string str16 = itemNode.GetAttribute("xmlns");
        //                if ("jabber:x:event".Equals(str16) && (tmpAttrbId != null))
        //                {
        //                    FMessage.Key key4 = new FMessage.Key(tmpNodeFrom, true, tmpAttrbId);
        //                    //if (this.EventHandler != null)
        //                    //{
        //                    //    ErrorHandler handler2 = null;
        //                    //    if (trackedMessages.TryGetValue(key4, out handler2))
        //                    //    {
        //                    //        trackedMessages.Remove(key4);
        //                    //        handler2.OnCompleted();
        //                    //    }
        //                    //    else
        //                    //    {
        //                    //        this.EventHandler.OnMessageStatusUpdate(key4, FMessage.Status.ReceivedByServer);
        //                    //    }
        //                    //}
        //                }
        //            }
        //            else if (ProtocolTreeNode.TagEquals(itemNode, "received"))
        //            {
        //                if (tmpAttrbId != null)
        //                {
        //                    FMessage.Key key5 = new FMessage.Key(tmpNodeFrom, true, tmpAttrbId);
        //                    //if (this.EventHandler != null)
        //                    //{
        //                    //    this.EventHandler.OnMessageStatusUpdate(key5, FMessage.Status.ReceivedByTarget);
        //                    //}
        //                    if (true)//this.SupportsReceiptAcks)
        //                    {
        //                        string str17 = itemNode.GetAttribute("type");
        //                        if ((str17 != null) && !str17.Equals("delivered"))
        //                        {
        //                            if (str17.Equals("visible"))
        //                            {
        //                                this.WhatsSendHandler.SendVisibleReceiptAck(tmpNodeFrom, tmpAttrbId);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            this.WhatsSendHandler.SendDeliveredReceiptAck(tmpNodeFrom, tmpAttrbId);
        //                        }
        //                    }
        //                }
        //            }
        //            else if (ProtocolTreeNode.TagEquals(itemNode, "offline"))
        //            {
        //                builder.Offline(true);
        //            }
        //        }
        //        if (!builder.Timestamp().HasValue)
        //        {
        //            builder.Timestamp(new DateTime?(DateTime.Now));
        //        }
        //        FMessage message = builder.Build();
        //        if ((message != null) && (this.MessageRecievedEvent != null))
        //        {
        //            this.MessageRecievedEvent(message);
        //            //this.EventHandler.OnMessageForMe(message, duplicate);
        //        }
        //    }
        //    else if ("notification".Equals(tmpNodeType))
        //    {
        //        bool flag3 = false;
        //        foreach (ProtocolTreeNode node6 in (messageNode.GetAllChildren() ?? new ProtocolTreeNode[0]))
        //        {
        //            if (ProtocolTreeNode.TagEquals(node6, "notification"))
        //            {
        //                string x = node6.GetAttribute("type");
        //                if (StringComparer.Ordinal.Equals(x, "picture") )//&& (this.EventHandler != null))
        //                {
        //                    foreach (ProtocolTreeNode node7 in (node6.GetAllChildren() ?? new ProtocolTreeNode[0]))
        //                    {
        //                        if (ProtocolTreeNode.TagEquals(node7, "set"))
        //                        {
        //                            string photoId = node7.GetAttribute("id");
        //                            if (photoId != null)
        //                            {
        //                                //this.EventHandler.OnPhotoChanged(str3, node7.GetAttribute("author"), photoId);
        //                            }
        //                        }
        //                        else if (ProtocolTreeNode.TagEquals(node7, "delete"))
        //                        {
        //                            //this.EventHandler.OnPhotoChanged(str3, node7.GetAttribute("author"), null);
        //                        }
        //                    }
        //                }
        //            }
        //            else if (ProtocolTreeNode.TagEquals(node6, "request"))
        //            {
        //                flag3 = true;
        //            }
        //        }
        //        if (flag3)
        //        {
        //            //this.whatsSendHandler.SendNotificationReceived(str3, attributeValue);
        //        }
        //    }
        //}

        internal static Dictionary<string, long> ParseCategories(ProtocolTreeNode dirtyNode)
        {
            var dictionary = new Dictionary<string, long>();
            if (dirtyNode.children != null)
            {
                for (int i = 0; i < dirtyNode.children.Count(); i++)
                {
                    ProtocolTreeNode node = dirtyNode.children.ElementAt(i);
                    if (ProtocolTreeNode.TagEquals(node, "category"))
                    {
                        long num2;
                        string attributeValue = node.GetAttribute("name");
                        if (long.TryParse(node.GetAttribute("timestamp"), WhatsConstants.WhatsAppNumberStyle,
                                          CultureInfo.InvariantCulture, out num2))
                        {
                            dictionary[attributeValue] = num2;
                        }
                    }
                }
            }
            return dictionary;
        }
    }
}
