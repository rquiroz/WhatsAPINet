using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;

namespace WhatsAppApi
{
    public class WhatsSendHandler
    {
        private string MyJID = "";
        private string whatsAppRealm = "s.whatsapp.net";
        private BinTreeNodeWriter _binWriter;
        private WhatsNetwork whatsNetwork; 
          
        internal WhatsSendHandler(WhatsNetwork net, BinTreeNodeWriter writer)
        {
            this.whatsNetwork = net;
            this._binWriter = writer;
        }

        public void SendActive()
        {
            var node = new ProtocolTreeNode("presence", new[] {new KeyValue("type", "active")});
            this.whatsNetwork.SendData(this._binWriter.Write(node));
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

        public void SendAvailableForChat(string nickName)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("name", nickName) });
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendClearDirty(IEnumerable<string> categoryNames)
        {
            string id = TicketCounter.MakeId("clean_dirty_");
            IEnumerable<ProtocolTreeNode> source = from category in categoryNames select new ProtocolTreeNode("category", new[] { new KeyValue("name", category) });
            var child = new ProtocolTreeNode("clean", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:dirty") }, source);
            var node = new ProtocolTreeNode("iq",
                                            new[]
                                                {
                                                    new KeyValue("id", id), new KeyValue("type", "set"),
                                                    new KeyValue("to", "s.whatsapp.net")
                                                }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendClearDirty(string category)
        {
            this.SendClearDirty(new string[] { category });
        }

        public void SendClientConfig(string platform, string lg, string lc)
        {
            string v = TicketCounter.MakeId("config_");
            var child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push"), new KeyValue("platform", platform), new KeyValue("lg", lg), new KeyValue("lc", lc) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", v), new KeyValue("type", "set"), new KeyValue("to", whatsAppRealm) }, new ProtocolTreeNode[] { child });
            this.whatsNetwork.SendData(this._binWriter.Write(node));
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
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendClose()
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unavailable") });
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendComposing(string to)
        {
            var child = new ProtocolTreeNode("composing", new[] { new KeyValue("xmlns", "http://jabber.org/protocol/chatstates") });
            var node = new ProtocolTreeNode("message", new[] { new KeyValue("to", to), new KeyValue("type", "chat") }, new ProtocolTreeNode[] {child});
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendCreateGroupChat(string subject)
        {
            this.SendCreateGroupChat(subject, null, null);
        }

        public void SendCreateGroupChat(string subject, Action<string> onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("create_group_");
            var child = new ProtocolTreeNode("group", new[] { new KeyValue("xmlns", "w:g"), new KeyValue("action", "create"), new KeyValue("subject", subject) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("to", "g.us") }, new ProtocolTreeNode[] {child});
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendDeleteAccount(Action onSuccess, Action<int> onError)
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
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendDeleteFromRoster(string jid)
        {
            string v = TicketCounter.MakeId("roster_");
            var innerChild = new ProtocolTreeNode("item", new[] { new KeyValue("jid", jid), new KeyValue("subscription", "remove") });
            var child = new ProtocolTreeNode("query", new[] { new KeyValue("xmlns", "jabber:iq:roster") }, new ProtocolTreeNode[] {innerChild});
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("type", "set"), new KeyValue("id", v) }, new ProtocolTreeNode[] {child});
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendDeliveredReceiptAck(string to, string id)
        {
            this.SendReceiptAck(to, id, "delivered");
        }

        public void SendEndGroupChat(string gjid)
        {
            this.SendEndGroupChat(gjid, null, null);
        }

        public void SendEndGroupChat(string gjid, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("remove_group_");
            var child = new ProtocolTreeNode("group", new[] { new KeyValue("xmlns", "w:g"), new KeyValue("action", "delete") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("to", gjid) }, new ProtocolTreeNode[] {child});
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendGetClientConfig()
        {
            string id = TicketCounter.MakeId("get_config_");
            var child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", this.whatsAppRealm) }, new ProtocolTreeNode[] { child });
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendGetDirty()
        {
            string id = TicketCounter.MakeId("get_dirty_");
            var child = new ProtocolTreeNode("status", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:dirty") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", "s.whatsapp.net") }, new ProtocolTreeNode[] {child});
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendGetGroupInfo(string gjid)
        {
            string id = TicketCounter.MakeId("get_g_info_");
            var child = new ProtocolTreeNode("query", new[] { new KeyValue("xmlns", "w:g") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", gjid) }, new ProtocolTreeNode[] {child});
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendGetGroups(Action onSuccess, Action<int> onError)
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
            var child = new ProtocolTreeNode("list", new[] { new KeyValue("xmlns", "w:g") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", gjid) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendGetPhoto(string jid, bool largeFormat)
        {
            this.SendGetPhoto(jid, null, largeFormat, delegate { });
        }

        public void SendGetPhoto(string jid, string expectedPhotoId, bool largeFormat, Action onComplete)
        {
            string id = TicketCounter.MakeId("get_photo_");
            var attrList = new List<KeyValue> { new KeyValue("xmlns", "w:profile:picture") };
            if (!largeFormat)
            {
                attrList.Add(new KeyValue("type", "preview"));
            }
            if (expectedPhotoId != null)
            {
                attrList.Add(new KeyValue("id", expectedPhotoId));
            }
            var child = new ProtocolTreeNode("picture", attrList.ToArray());
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", jid) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendGetPhotoIds(IEnumerable<string> jids)
        {
            string id = TicketCounter.MakeId("get_photo_id_");
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", this.MyJID) },
                new ProtocolTreeNode("list", new[] { new KeyValue("xmlns", "w:profile:picture") },
                    (from jid in jids select new ProtocolTreeNode("user", new[] { new KeyValue("jid", jid) })).ToArray<ProtocolTreeNode>()));
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendGetPrivacyList()
        {
            string id = TicketCounter.MakeId("privacylist_");
            var innerChild = new ProtocolTreeNode("list", new[] { new KeyValue("name", "default") });
            var child = new ProtocolTreeNode("query", new KeyValue[] { new KeyValue("xmlns", "jabber:iq:privacy") }, innerChild);
            var node = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "get") }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendGetServerProperties()
        {
            string id = TicketCounter.MakeId("get_server_properties_");
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", "s.whatsapp.net") },
                new ProtocolTreeNode("props", new[] { new KeyValue("xmlns", "w") }));
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendGetStatus(string jid)
        {
            int index = jid.IndexOf('@');
            if (index > 0)
            {
                jid = string.Format("{0}@{1}", jid.Substring(0, index), "s.us");
                string v = TicketManager.GenerateId();
                var node = new ProtocolTreeNode("message", new[] { new KeyValue("to", jid), new KeyValue("type", "action"), new KeyValue("id", v) },
                    new ProtocolTreeNode("action", new[] { new KeyValue("type", "get") }));
                this.whatsNetwork.SendData(this._binWriter.Write(node));
            }
        }

        public void SendInactive()
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "inactive") });
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendLeaveGroup(string gjid)
        {
            this.SendLeaveGroup(gjid, null, null);
        }

        public void SendLeaveGroup(string gjid, Action onSuccess, Action<int> onError)
        {
            this.SendLeaveGroups(new string[] { gjid }, onSuccess, onError);
        }

        public void SendLeaveGroups(IEnumerable<string> gjids, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("leave_group_");
            IEnumerable<ProtocolTreeNode> innerChilds = from gjid in gjids select new ProtocolTreeNode("group", new[] { new KeyValue("id", gjid) });
            var child = new ProtocolTreeNode("leave", new KeyValue[] { new KeyValue("xmlns", "w:g") }, innerChilds);
            var node = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("to", "g.us") }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendMessage(FMessage message)
        {
            if (message.media_wa_type != FMessage.Type.Undefined)
            {
                this.SendMessageWithMedia(message);
            }
            else
            {
                this.SendMessageWithBody(message);
            }
        }

        public void SendMessageReceived(FMessage message)
        {
            var child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            var node = new ProtocolTreeNode("message", new[] { new KeyValue("to", message.key.remote_jid), new KeyValue("type", "chat"), new KeyValue("id", message.key.id) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendNop()
        {
            this.whatsNetwork.SendData(this._binWriter.Write(null));
        }

        public void SendNotificationReceived(string jid, string id)
        {
            var child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            var node = new ProtocolTreeNode("message", new[] { new KeyValue("to", jid), new KeyValue("type", "notification"), new KeyValue("id", id) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendPaused(string to)
        {
            var child = new ProtocolTreeNode("paused", new[] { new KeyValue("xmlns", "http://jabber.org/protocol/chatstates") });
            var node = new ProtocolTreeNode("message", new[] { new KeyValue("to", to), new KeyValue("type", "chat") }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendPing()
        {
            string id = TicketCounter.MakeId("ping_");
            var child = new ProtocolTreeNode("ping", new[] { new KeyValue("xmlns", "w:p") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get") }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendPong(string id)
        {
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("type", "result"), new KeyValue("to", this.whatsAppRealm), new KeyValue("id", id) });
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendPresenceSubscriptionRequest(string to)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "subscribe"), new KeyValue("to", to) });
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendQueryLastOnline(string jid)
        {
            string id = TicketCounter.MakeId("last_");
            var child = new ProtocolTreeNode("query", new[] { new KeyValue("xmlns", "jabber:iq:last") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", jid) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendRelayCapable(string platform, bool value)
        {
            string v = TicketCounter.MakeId("relay_");
            var child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push"), new KeyValue("platform", platform), new KeyValue("relay", value ? "1" : "0") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", v), new KeyValue("type", "set"), new KeyValue("to", this.whatsAppRealm) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendRelayComplete(string id, int millis)
        {
            var child = new ProtocolTreeNode("relay", new[] { new KeyValue("elapsed", millis.ToString(CultureInfo.InvariantCulture)) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("xmlns", "w:p:r"), new KeyValue("type", "result"), new KeyValue("to", "s.whatsapp.net"), new KeyValue("id", id) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendRelayTimeout(string id)
        {
            var innerChild = new ProtocolTreeNode("remote-server-timeout", new[] { new KeyValue("xmlns", "urn:ietf:params:xml:ns:xmpp-stanzas") });
            var child = new ProtocolTreeNode("error", new[] { new KeyValue("code", "504"), new KeyValue("type", "wait") }, innerChild);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("xmlns", "w:p:r"), new KeyValue("type", "error"), new KeyValue("to", "s.whatsapp.net"), new KeyValue("id", id) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendRemoveParticipants(string gjid, List<string> participants)
        {
            this.SendRemoveParticipants(gjid, participants, null, null);
        }

        public void SendRemoveParticipants(string gjid, List<string> participants, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("remove_group_participants_");
            this.SendVerbParticipants(gjid, participants, id, "remove");
        }

        public void SendSetGroupSubject(string gjid, string subject)
        {
            this.SendSetGroupSubject(gjid, subject, null, null);
        }

        public void SendSetGroupSubject(string gjid, string subject, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("set_group_subject_");
            var child = new ProtocolTreeNode("subject", new[] { new KeyValue("xmlns", "w:g"), new KeyValue("value", subject) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("to", gjid) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendSetPhoto(string jid, byte[] bytes, byte[] thumbnailBytes, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("set_photo_");
            var list = new List<ProtocolTreeNode> { new ProtocolTreeNode("picture", new[] { new KeyValue("xmlns", "w:profile:picture") }, null, bytes) };
            if (thumbnailBytes != null)
            {
                list.Add(new ProtocolTreeNode("picture", new[] { new KeyValue("type", "preview") }, null, thumbnailBytes));
            }
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("to", jid) }, list.ToArray());
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendSetPrivacyBlockedList(IEnumerable<string> list)
        {
            this.SendSetPrivacyBlockedList(list, null, null);
        }

        public void SendSetPrivacyBlockedList(IEnumerable<string> jidSet, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("privacy_");
            ProtocolTreeNode[] nodeArray = Enumerable.Select<string, ProtocolTreeNode>(jidSet, (Func<string, int, ProtocolTreeNode>)((jid, index) => new ProtocolTreeNode("item", new KeyValue[] { new KeyValue("type", "jid"), new KeyValue("value", jid), new KeyValue("action", "deny"), new KeyValue("order", index.ToString(CultureInfo.InvariantCulture)) }))).ToArray<ProtocolTreeNode>();
            var child = new ProtocolTreeNode("list", new KeyValue[] { new KeyValue("name", "default") }, (nodeArray.Length == 0) ? null : nodeArray);
            var node2 = new ProtocolTreeNode("query", new KeyValue[] { new KeyValue("xmlns", "jabber:iq:privacy") }, child);
            var node3 = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "set") }, node2);
            this.whatsNetwork.SendData(this._binWriter.Write(node3));
        }

        public void SendStatusUpdate(string status, Action onComplete, Action<int> onError)
        {
            string id = TicketManager.GenerateId();
            FMessage message = new FMessage(new FMessage.Key("s.us", true, id));
            var messageNode = GetMessageNode(message, new ProtocolTreeNode("body", null, WhatsApp.SYSEncoding.GetBytes(status)));
            this.whatsNetwork.SendData(this._binWriter.Write(messageNode));
        }

        public void SendSubjectReceived(string to, string id)
        {
            var child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            var node = GetSubjectMessage(to, id, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendUnsubscribeHim(string jid)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unsubscribed"), new KeyValue("to", jid) });
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendUnsubscribeMe(string jid)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unsubscribe"), new KeyValue("to", jid) });
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        public void SendVisibleReceiptAck(string to, string id)
        {
            this.SendReceiptAck(to, id, "visible");
        }

        internal void SendGetGroups(string id, string type)
        {
            var child = new ProtocolTreeNode("list", new[] { new KeyValue("xmlns", "w:g"), new KeyValue("type", type) });
            var  node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", "g.us") }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        internal void SendMessageWithBody(FMessage message)
        {
            var child = new ProtocolTreeNode("body", null, null, WhatsApp.SYSEncoding.GetBytes(message.data));
            this.whatsNetwork.SendData(this._binWriter.Write(GetMessageNode(message, child)));
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
            this.whatsNetwork.SendData(this._binWriter.Write(node));
        }

        internal void SendVerbParticipants(string gjid, IEnumerable<string> participants, string id, string inner_tag)
        {
            IEnumerable<ProtocolTreeNode> source = from jid in participants select new ProtocolTreeNode("participant", new[] { new KeyValue("jid", jid) });
            var child = new ProtocolTreeNode(inner_tag, new[] { new KeyValue("xmlns", "w:g") }, source);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("to", gjid) }, child);
            this.whatsNetwork.SendData(this._binWriter.Write(node));
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
                    new KeyValue("mute", string.Format(CultureInfo.InvariantCulture, "{0}", new object[] { (!@group.MuteExpiry.HasValue || (@group.MuteExpiry.Value <= now)) ? 0 : ((int) (@group.MuteExpiry.Value - now).TotalSeconds) })) })).ToArray<ProtocolTreeNode>();
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
            this.whatsNetwork.SendData(this._binWriter.Write(resultNode));
        }

        internal static ProtocolTreeNode GetMessageNode(FMessage message, ProtocolTreeNode pNode)
        {
            return new ProtocolTreeNode("message", new[] { new KeyValue("to", message.key.remote_jid), new KeyValue("type", "chat"), new KeyValue("id", message.key.id) }, pNode);
        }

        public static ProtocolTreeNode GetSubjectMessage(string to, string id, ProtocolTreeNode child)
        {
            return new ProtocolTreeNode("message", new[] { new KeyValue("to", to), new KeyValue("type", "subject"), new KeyValue("id", id) }, child);
        }
    }
}