using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Settings;

namespace WhatsAppApi.Response
{
    /// <summary>
    /// Respond to a recieved message
    /// </summary>
    class MessageRecvResponse
    {
        /// <summary>
        /// An instance of the WhatsSendHandler class
        /// </summary>
        private WhatsSendHandler sendHandler;

        /// <summary>
        /// Default class constructor
        /// </summary>
        /// <param name="sendHandler">An instance of the WhatsSendHandler class</param>
        public MessageRecvResponse(WhatsSendHandler sendHandler)
        {
            this.sendHandler = sendHandler;
        }

        /// <summary>
        /// Parse recieved message
        /// </summary>
        /// <param name="messageNode">TreeNode that contains the recieved message</param>
        public void ParseMessageRecv(ProtocolTreeNode messageNode)
        {
            FMessage.Builder builder = new FMessage.Builder();
            string tmpAttrbId = messageNode.GetAttribute("id");
            string tmpAttrFrom = messageNode.GetAttribute("from");
            string tmpAttrFromName = messageNode.GetAttribute("");
            string tmpAttrFromJid = messageNode.GetAttribute("author") ?? "";
            string tmpAttrType = messageNode.GetAttribute("type");
            string tmpTAttribT = messageNode.GetAttribute("t");

            long result = 0L;
            if (!string.IsNullOrEmpty(tmpTAttribT) && long.TryParse(tmpTAttribT, out result))
            {
                builder.Timestamp(new DateTime?(WhatsConstants.UnixEpoch.AddSeconds((double)result)));
            }

            if ("error".Equals(tmpAttrType))
            {
                TypeError(messageNode, tmpAttrbId, tmpAttrFrom);
            }
            else if ("subject".Equals(tmpAttrType))
            {
                TypeSubject(messageNode, tmpAttrFrom, tmpAttrFromJid, tmpAttrbId, tmpTAttribT);
            }
            else if ("chat".Equals(tmpAttrType))
            {
                TypeChat(messageNode, tmpAttrFrom, tmpAttrbId, builder, tmpAttrFromJid);
            }
            else if ("notification".Equals(tmpAttrType))
            {
                TypeNotification(messageNode, tmpAttrFrom, tmpAttrbId);
            }
        }

        /// <summary>
        /// Notify typing
        /// </summary>
        /// <param name="messageNode">The protocoltreenode</param>
        /// <param name="tmpAttrFrom">From?</param>
        /// <param name="tmpAttrbId">Message id</param>
        private void TypeNotification(ProtocolTreeNode messageNode, string tmpAttrFrom, string tmpAttrbId)
        {
            foreach (ProtocolTreeNode tmpChild in (messageNode.GetAllChildren() ?? new ProtocolTreeNode[0]))
            {
                if (ProtocolTreeNode.TagEquals(tmpChild, "notification"))
                {
                    string tmpChildType = tmpChild.GetAttribute("type");
                    if (StringComparer.Ordinal.Equals(tmpChildType, "picture"))
                    {
                        TypeNotificationPicture(tmpChild, tmpAttrFrom);
                    }
                }
                else if (ProtocolTreeNode.TagEquals(tmpChild, "request"))
                {
                    this.sendHandler.SendNotificationReceived(tmpAttrFrom, tmpAttrbId);
                }
            }
        }

        /// <summary>
        /// Notify typing picture
        /// </summary>
        /// <param name="tmpChild">Child</param>
        /// <param name="tmpFrom">From?</param>
        private static void TypeNotificationPicture(ProtocolTreeNode tmpChild, string tmpFrom)
        {
            foreach (ProtocolTreeNode item in (tmpChild.GetAllChildren() ?? new ProtocolTreeNode[0]))
            {
                if (ProtocolTreeNode.TagEquals(item, "set"))
                {
                    string photoId = item.GetAttribute("id");
                    if (photoId != null)
                    {
                        WhatsEventHandler.OnPhotoChangedEventHandler(tmpFrom, item.GetAttribute("author"), photoId);
                    }
                }
                else if (ProtocolTreeNode.TagEquals(item, "delete"))
                {
                    WhatsEventHandler.OnPhotoChangedEventHandler(tmpFrom, item.GetAttribute("author"), null);
                }
            }
        }

        /// <summary>
        /// Notify typing chat
        /// </summary>
        /// <param name="messageNode"></param>
        /// <param name="tmpAttrFrom"></param>
        /// <param name="tmpAttrbId"></param>
        /// <param name="builder"></param>
        /// <param name="tmpAttrFromJid"></param>
        private void TypeChat(ProtocolTreeNode messageNode, string tmpAttrFrom, string tmpAttrbId, FMessage.Builder builder, string tmpAttrFromJid)
        {
            foreach (ProtocolTreeNode itemNode in (messageNode.GetAllChildren() ?? new ProtocolTreeNode[0]))
            {
                if (ProtocolTreeNode.TagEquals(itemNode, "composing"))
                {
                    WhatsEventHandler.OnIsTypingEventHandler(tmpAttrFrom, true);
                }
                else if (ProtocolTreeNode.TagEquals(itemNode, "paused"))
                {
                    WhatsEventHandler.OnIsTypingEventHandler(tmpAttrFrom, false);
                }
                else if (ProtocolTreeNode.TagEquals(itemNode, "body") && (tmpAttrbId != null))
                {
                    string dataString = WhatsApp.SYSEncoding.GetString(itemNode.GetData());
                    var tmpMessKey = new FMessage.FMessageIdentifierKey(tmpAttrFrom, false, tmpAttrbId);
                    builder.Key(tmpMessKey).Remote_resource(tmpAttrFromJid).NewIncomingInstance().Data(dataString);
                }
                else if (ProtocolTreeNode.TagEquals(itemNode, "media") && (tmpAttrbId != null))
                {
                    long tmpMediaSize;
                    int tmpMediaDuration;

                    builder.Media_wa_type(FMessage.GetMessage_WA_Type(itemNode.GetAttribute("type"))).Media_url(
                        itemNode.GetAttribute("url")).Media_name(itemNode.GetAttribute("file"));

                    if (long.TryParse(itemNode.GetAttribute("size"), WhatsConstants.WhatsAppNumberStyle,
                                      CultureInfo.InvariantCulture, out tmpMediaSize))
                    {
                        builder.Media_size(tmpMediaSize);
                    }
                    string tmpAttrSeconds = itemNode.GetAttribute("seconds");
                    if ((tmpAttrSeconds != null) &&
                        int.TryParse(tmpAttrSeconds, WhatsConstants.WhatsAppNumberStyle, CultureInfo.InvariantCulture, out tmpMediaDuration))
                    {
                        builder.Media_duration_seconds(tmpMediaDuration);
                    }

                    if (builder.Media_wa_type().HasValue && (builder.Media_wa_type().Value == FMessage.Type.Location))
                    {
                        double tmpLatitude = 0;
                        double tmpLongitude = 0;
                        string tmpAttrLatitude = itemNode.GetAttribute("latitude");
                        string tmpAttrLongitude = itemNode.GetAttribute("longitude");
                        if ((tmpAttrLatitude == null) || (tmpAttrLongitude == null))
                        {
                            tmpAttrLatitude = "0";
                            tmpAttrLongitude = "0";
                        }
                        else if (!double.TryParse(tmpAttrLatitude, WhatsConstants.WhatsAppNumberStyle, CultureInfo.InvariantCulture, out tmpLatitude) ||
                            !double.TryParse(tmpAttrLongitude, WhatsConstants.WhatsAppNumberStyle, CultureInfo.InvariantCulture, out tmpLongitude))
                        {
                            throw new CorruptStreamException("location message exception parsing lat or long attribute: " + tmpAttrLatitude + " " + tmpAttrLongitude);
                        }

                        builder.Latitude(tmpLatitude).Longitude(tmpLongitude);

                        string tmpAttrName = itemNode.GetAttribute("name");
                        string tmpAttrUrl = itemNode.GetAttribute("url");
                        if (tmpAttrName != null)
                        {
                            builder.Location_details(tmpAttrName);
                        }
                        if (tmpAttrUrl != null)
                        {
                            builder.Location_url(tmpAttrUrl);
                        }
                    }

                    if (builder.Media_wa_type().HasValue && (builder.Media_wa_type().Value) == FMessage.Type.Contact)
                    {
                        ProtocolTreeNode tmpChildMedia = itemNode.GetChild("media");
                        if (tmpChildMedia != null)
                        {
                            builder.Media_name(tmpChildMedia.GetAttribute("name")).Data(WhatsApp.SYSEncoding.GetString(tmpChildMedia.GetData()));
                        }
                    }
                    else
                    {
                        string tmpAttrEncoding = itemNode.GetAttribute("encoding") ?? "text";
                        if (tmpAttrEncoding == "text")
                        {
                            builder.Data(WhatsApp.SYSEncoding.GetString(itemNode.GetData()));
                        }
                    }
                    var tmpMessageKey = new FMessage.FMessageIdentifierKey(tmpAttrFrom, false, tmpAttrbId);
                    builder.Key(tmpMessageKey).Remote_resource(tmpAttrFromJid).NewIncomingInstance();
                }
                else if (ProtocolTreeNode.TagEquals(itemNode, "request"))
                {
                    builder.Wants_receipt(true);
                }
                else if (ProtocolTreeNode.TagEquals(itemNode, "x"))
                {
                    string str16 = itemNode.GetAttribute("xmlns");
                    if ("jabber:x:event".Equals(str16) && (tmpAttrbId != null))
                    {
                        var tmpMessageKey = new FMessage.FMessageIdentifierKey(tmpAttrFrom, true, tmpAttrbId);
                    }
                }
                else if (ProtocolTreeNode.TagEquals(itemNode, "received"))
                {
                    if (tmpAttrbId != null)
                    {
                        var tmpMessageKey = new FMessage.FMessageIdentifierKey(tmpAttrFrom, true, tmpAttrbId);
                        if (true) 
                        {
                            string tmpAttrType = itemNode.GetAttribute("type");
                            if ((tmpAttrType != null) && !tmpAttrType.Equals("delivered"))
                            {
                                if (tmpAttrType.Equals("visible"))
                                {
                                    this.sendHandler.SendVisibleReceiptAck(tmpAttrFrom, tmpAttrbId);
                                }
                            }
                            else
                            {
                                this.sendHandler.SendDeliveredReceiptAck(tmpAttrFrom, tmpAttrbId);
                            }
                        }
                    }
                }
                else if (ProtocolTreeNode.TagEquals(itemNode, "offline"))
                {
                    builder.Offline(true);
                }
                else if (ProtocolTreeNode.TagEquals(itemNode, "notify"))
                {
                    var tmpAttrName = itemNode.GetAttribute("name");
                    if (tmpAttrName != null)
                    {
                        builder.from_me = false;
                        builder.id = tmpAttrbId;
                        builder.remote_jid = tmpAttrFromJid;
                        builder.serverNickname = tmpAttrName;
                    }
                }
            }
            if (!builder.Timestamp().HasValue)
            {
                builder.Timestamp(new DateTime?(DateTime.Now));
            }
            FMessage message = builder.Build();
            if (message != null)
            {
                WhatsEventHandler.OnMessageRecievedEventHandler(message);
            }
        }

        /// <summary>
        /// Type subject
        /// </summary>
        /// <param name="messageNode"></param>
        /// <param name="tmpFrom"></param>
        /// <param name="uJid"></param>
        /// <param name="tmpId"></param>
        /// <param name="tmpT"></param>
        private void TypeSubject(ProtocolTreeNode messageNode, string tmpFrom, string uJid, string tmpId, string tmpT)
        {
            bool flag = false;
            foreach (ProtocolTreeNode item in messageNode.GetAllChildren("request"))
            {
                if (item.GetAttribute("xmlns").Equals("urn:xmpp:receipts"))
                {
                    flag = true;
                }
            }
            ProtocolTreeNode child = messageNode.GetChild("body");
            string subject = (child == null) ? null : WhatsApp.SYSEncoding.GetString(child.GetData());
            if (subject != null)
            {
                WhatsEventHandler.OnGroupNewSubjectEventHandler(tmpFrom, uJid, subject, int.Parse(tmpT, CultureInfo.InvariantCulture));
            }
            if (flag)
            {
                this.sendHandler.SendSubjectReceived(tmpFrom, tmpId);
            }
        }

        /// <summary>
        /// Type error
        /// </summary>
        /// <param name="messageNode"></param>
        /// <param name="tmpAttrbId"></param>
        /// <param name="tmpAttrFrom"></param>
        private void TypeError(ProtocolTreeNode messageNode, string tmpAttrbId, string tmpAttrFrom)
        {
            int num2 = 0;
            foreach (ProtocolTreeNode node in messageNode.GetAllChildren("error"))
            {
                string tmpCode = node.GetAttribute("code");
                try
                {
                    num2 = int.Parse(tmpCode, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                }
            }
            if ((tmpAttrFrom != null) && (tmpAttrbId != null))
            {
                FMessage.FMessageIdentifierKey key = new FMessage.FMessageIdentifierKey(tmpAttrFrom, true, tmpAttrbId);
            }
        }
    }
}
