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
    /// <summary>
    /// Handles sending messages to the Whatsapp servers
    /// </summary>
    public class WhatsSendHandler
    {
        /// <summary>
        /// Holds the jabber id that is used to authenticate
        /// </summary>
        private string MyJID = "";

        /// <summary>
        /// The whatsapp realm, defined in WhatsConstants.
        /// </summary>
        private string whatsAppRealm = WhatsAppApi.Settings.WhatsConstants.WhatsAppRealm;

        /// <summary>
        /// Holds an instance of the BinTreeNodeWriter
        /// </summary>
        internal BinTreeNodeWriter BinWriter;

        /// <summary>
        /// Holds an instance of the WhatsNetwork class
        /// </summary>
        private WhatsNetwork whatsNetwork; 
          
        /// <summary>
        /// Default class constructor
        /// </summary>
        /// <param name="net">An instance of the WhatsNetwork class</param>
        /// <param name="writer">An instance of the BinTreeNodeWriter</param>
        internal WhatsSendHandler(WhatsNetwork net, BinTreeNodeWriter writer)
        {
            this.whatsNetwork = net;
            this.BinWriter = writer;
        }

        /// <summary>
        /// Sends a message to the Whatsapp server to tell that the user is 'online' / 'active'
        /// </summary>
        public void SendActive()
        {
            var node = new ProtocolTreeNode("presence", new[] {new KeyValue("type", "active")});
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Sends a request to the server to add participants to a group chat
        /// </summary>
        /// <param name="gjid">Group jabber id</param>
        /// <param name="participants">A list of participants (List of jids)</param>
        public void SendAddParticipants(string gjid, IEnumerable<string> participants)
        {
            this.SendAddParticipants(gjid, participants, null, null);
        }

        /// <summary>
        /// Sends a request to the server to add participants to a group chat
        /// </summary>
        /// <param name="gjid">Group jabber id</param>
        /// <param name="participants">A list of participants (List of jids)</param>
        /// <param name="onSuccess">The action to be executed when the request is successfull.</param>
        /// <param name="onError">The action to be executed when the request fails</param>
        public void SendAddParticipants(string gjid, IEnumerable<string> participants, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("add_group_participants_");
            this.SendVerbParticipants(gjid, participants, id, "add");
        }

        public void SendAvailableForChat(string nickName, bool isHidden = false)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("name", nickName) });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        public void SendUnavailable()
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unavailable") });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
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

        /// <summary>
        /// Sends the client configuration to the Whatsapp server.
        /// </summary>
        /// <param name="platform">The string identifying the client.</param>
        /// <param name="lg">?</param>
        /// <param name="lc">?</param>
        public void SendClientConfig(string platform, string lg, string lc)
        {
            string v = TicketCounter.MakeId("config_");
            var child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push"), new KeyValue("platform", platform), new KeyValue("lg", lg), new KeyValue("lc", lc) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", v), new KeyValue("type", "set"), new KeyValue("to", whatsAppRealm) }, new ProtocolTreeNode[] { child });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Sends the client configuration to the Whatsapp server.
        /// </summary>
        /// <param name="platform">The string identifying the client.</param>
        /// <param name="lg">?</param>
        /// <param name="lc">?</param>
        /// <param name="pushUri">?</param>
        /// <param name="preview">?</param>
        /// <param name="defaultSetting">Default settings.</param>
        /// <param name="groupsSetting">Settings regarding groups.</param>
        /// <param name="groups">A list of groups</param>
        /// <param name="onCompleted">Action to be executed when the request was successfull.</param>
        /// <param name="onError">Action to be executed when the request failed.</param>
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
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Change status to 'Offline'
        /// </summary>
        public void SendClose()
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unavailable") });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Send the status of 'writing'/'typing'/'composing' to the Whatsapp server
        /// </summary>
        /// <param name="to">The recipient, the one the client is talking to.</param>
        public void SendComposing(string to)
        {
            this.SendChatState(to, "composing");
        }

        protected void SendChatState(string to, string type)
        {
            var node = new ProtocolTreeNode("chatstate", new[] { new KeyValue("to", WhatsApp.GetJID(to)) }, new [] { 
                new ProtocolTreeNode(type, null)
            });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Requests to create a new and empty group chat.
        /// </summary>
        /// <param name="subject">The subject of the group chat.</param>
        public void SendCreateGroupChat(string subject)
        {
            this.SendCreateGroupChat(subject, null, null);
        }

        /// <summary>
        /// Requests to create a new and empty group chat.
        /// </summary>
        /// <param name="subject">The subjecct of the group chat.</param>
        /// <param name="onSuccess">Acction to be executed when the request was successful.</param>
        /// <param name="onError">Action to be executed when the request failed.</param>
        public void SendCreateGroupChat(string subject, Action<string> onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("create_group_");
            var child = new ProtocolTreeNode("group", new[] { new KeyValue("action", "create"), new KeyValue("subject", subject) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", "g.us") }, new ProtocolTreeNode[] { child });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Make a reques to the Whatsapp servers to delete a specific account.
        /// </summary>
        /// <param name="onSuccess">The action to be executed when the request was successful.</param>
        /// <param name="onError">The action to be executed when the request failed.</param>
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
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Request to be delete from a group chat.
        /// </summary>
        /// <param name="jid"></param>
        public void SendDeleteFromRoster(string jid)
        {
            string v = TicketCounter.MakeId("roster_");
            var innerChild = new ProtocolTreeNode("item", new[] { new KeyValue("jid", jid), new KeyValue("subscription", "remove") });
            var child = new ProtocolTreeNode("query", new[] { new KeyValue("xmlns", "jabber:iq:roster") }, new ProtocolTreeNode[] {innerChild});
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("type", "set"), new KeyValue("id", v) }, new ProtocolTreeNode[] {child});
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Sends the 'Delivered' status to a specific client
        /// </summary>
        /// <param name="to">The JID of the person the chat is with.</param>
        /// <param name="id">The message id.</param>
        public void SendDeliveredReceiptAck(string to, string id)
        {
            this.SendReceiptAck(to, id, "delivered");
        }

        /// <summary>
        /// Sends a request to end and remove the group chat.
        /// </summary>
        /// <param name="gjid">The group jabber id.</param>
        public void SendEndGroupChat(string gjid)
        {
            this.SendEndGroupChat(gjid, null, null);
        }

        /// <summary>
        /// Sends a request to end and remove the group chat.
        /// </summary>
        /// <param name="gjid">The group jabber id.</param>
        /// <param name="onSuccess">The action to be executed when the request was successful.</param>
        /// <param name="onError">The action to be executed when the request failed.</param>
        public void SendEndGroupChat(string gjid, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("remove_group_");
            var child = new ProtocolTreeNode("group", new[] { new KeyValue("action", "delete") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", gjid) }, new ProtocolTreeNode[] { child });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Request the client confguration
        /// </summary>
        public void SendGetClientConfig()
        {
            string id = TicketCounter.MakeId("get_config_");
            var child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", this.whatsAppRealm) }, new ProtocolTreeNode[] { child });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Request dirty
        /// </summary>
        public void SendGetDirty()
        {
            string id = TicketCounter.MakeId("get_dirty_");
            var child = new ProtocolTreeNode("status", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:dirty") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", "s.whatsapp.net") }, new ProtocolTreeNode[] {child});
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Send a request to retrieve group information
        /// </summary>
        /// <param name="gjid">The group jabber id</param>
        public void SendGetGroupInfo(string gjid)
        {
            string id = TicketCounter.MakeId("get_g_info_");
            var child = new ProtocolTreeNode("query", null);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g"), new KeyValue("to", WhatsAppApi.WhatsApp.GetJID(gjid)) }, new ProtocolTreeNode[] { child });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Make a request to retrieve all groups where the client is participating in
        /// </summary>
        /// <param name="onSuccess">The action to be executed when the request was successful.</param>
        /// <param name="onError">The action to be executed when the request failed.</param>
        public void SendGetGroups(Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("get_groups_");
            this.SendGetGroups(id, "participating");
        }

        /// <summary>
        /// Make a request to retrieve all groups where the client is the owner of.
        /// </summary>
        public void SendGetOwningGroups()
        {
            string id = TicketCounter.MakeId("get_owning_groups_");
            this.SendGetGroups(id, "owning");
        }

        /// <summary>
        /// Make a request to retrieve all group participents
        /// </summary>
        /// <param name="gjid">The group jabber id</param>
        public void SendGetParticipants(string gjid)
        {
            string id = TicketCounter.MakeId("get_participants_");
            var child = new ProtocolTreeNode("list", null);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g"), new KeyValue("to", WhatsApp.GetJID(gjid)) }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Make a request to retrieve a photo
        /// </summary>
        /// <param name="jid">The group jabber id.</param>
        /// <param name="largeFormat">If set to true, the photo will be retrieved in the highest resolution.</param>
        public string SendGetPhoto(string jid, bool largeFormat)
        {
            return this.SendGetPhoto(jid, null, largeFormat, delegate { });
        }

        /// <summary>
        /// Make a request to retrieve a photo for a specific photo id.
        /// </summary>
        /// <param name="jid">The group jabber id.</param>
        /// <param name="expectedPhotoId">The specific photo that needs to be retrieved.</param>
        /// <param name="largeFormat">If set to true, the photo will be retrieved in the highest resolution.</param>
        /// <param name="onComplete">The action to be executed when the request was successful.</param>
        public string SendGetPhoto(string jid, string expectedPhotoId, bool largeFormat, Action onComplete)
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
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", WhatsAppApi.WhatsApp.GetJID(jid)) }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
            return id;
        }

        /// <summary>
        /// Make a request to retrieve a list of photo id's
        /// </summary>
        /// <param name="jids">The list of jabber id's the photos need to be retrieved of.</param>
        public void SendGetPhotoIds(IEnumerable<string> jids)
        {
            string id = TicketCounter.MakeId("get_photo_id_");
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", this.MyJID) },
                new ProtocolTreeNode("list", new[] { new KeyValue("xmlns", "w:profile:picture") },
                    (from jid in jids select new ProtocolTreeNode("user", new[] { new KeyValue("jid", jid) })).ToArray<ProtocolTreeNode>()));
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Make a request to retrieve a list of privacy's
        /// </summary>
        public void SendGetPrivacyList()
        {
            string id = TicketCounter.MakeId("privacylist_");
            var innerChild = new ProtocolTreeNode("list", new[] { new KeyValue("name", "default") });
            var child = new ProtocolTreeNode("query", new KeyValue[] { new KeyValue("xmlns", "jabber:iq:privacy") }, innerChild);
            var node = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "get") }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Make a request to retrieve information (properties) about the server.
        /// </summary>
        public void SendGetServerProperties()
        {
            string id = TicketCounter.MakeId("get_server_properties_");
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", "s.whatsapp.net") },
                new ProtocolTreeNode("props", new[] { new KeyValue("xmlns", "w") }));
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Make a request to retrieve the status for specific jabber id.
        /// </summary>
        /// <param name="jid">The jabber id the the status should be retrieved from.</param>
        public void SendGetStatus(string jid)
        {
            int index = jid.IndexOf('@');
            if (index > 0)
            {
                jid = string.Format("{0}@{1}", jid.Substring(0, index), "s.us");
                string v = TicketManager.GenerateId();
                var node = new ProtocolTreeNode("message", new[] { new KeyValue("to", jid), new KeyValue("type", "action"), new KeyValue("id", v) },
                    new ProtocolTreeNode("action", new[] { new KeyValue("type", "get") }));
                this.whatsNetwork.SendData(this.BinWriter.Write(node));
            }
        }

        /// <summary>
        /// Make a request to change our status to inactive.
        /// </summary>
        public void SendInactive()
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "inactive") });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Make a request to leave a group chat
        /// </summary>
        /// <param name="gjid">The group jabber id</param>
        public void SendLeaveGroup(string gjid)
        {
            this.SendLeaveGroup(gjid, null, null);
        }

        /// <summary>
        /// Make a rquest to leave a group chat
        /// </summary>
        /// <param name="gjid">The group jabber id.</param>
        /// <param name="onSuccess">The action to be executed when the request was successful.</param>
        /// <param name="onError">The action to be executed when the request failed.</param>
        public void SendLeaveGroup(string gjid, Action onSuccess, Action<int> onError)
        {
            this.SendLeaveGroups(new string[] { gjid }, onSuccess, onError);
        }

        /// <summary>
        /// Make a request to leave multiple groups at the same time.
        /// </summary>
        /// <param name="gjids">The group jabber id.</param>
        /// <param name="onSuccess">The action to be executed when the request was successful.</param>
        /// <param name="onError">The action to be executed when the request failed.</param>
        public void SendLeaveGroups(IEnumerable<string> gjids, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("leave_group_");
            IEnumerable<ProtocolTreeNode> innerChilds = from gjid in gjids select new ProtocolTreeNode("group", new[] { new KeyValue("id", gjid) });
            var child = new ProtocolTreeNode("leave", null, innerChilds);
            var node = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", "g.us") }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Sends a message, message properties are defined in the instance of FMessage.
        /// </summary>
        /// <param name="message">An instance of the FMessage class.</param>
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
            this.SendMessageBroadcast(to, new FMessage(string.Empty, true) { data = message, media_wa_type = FMessage.Type.Undefined});
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

        /// <summary>
        /// Tell the server the message has been recieved.
        /// </summary>
        /// <param name="message">An instance of the FMessage class.</param>
        public void SendMessageReceived(FMessage message, string response)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("receipt", new[] {
                new KeyValue("to", message.identifier_key.remote_jid),
                new KeyValue("id", message.identifier_key.id)
            });
            
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Send a null byte to the server
        /// </summary>
        public void SendNop()
        {
            this.whatsNetwork.SendData(this.BinWriter.Write(null));
        }

        /// <summary>
        /// Send a notification to a specific user that the message has been recieved
        /// </summary>
        /// <param name="jid">The jabber id</param>
        /// <param name="id">The id of the message</param>
        public void SendNotificationReceived(string jid, string id)
        {
            var child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            var node = new ProtocolTreeNode("message", new[] { new KeyValue("to", jid), new KeyValue("type", "notification"), new KeyValue("id", id) }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Send pause
        /// </summary>
        /// <param name="to">The jabber id of the reciever</param>
        public void SendPaused(string to)
        {
            this.SendChatState(to, "paused");
        }

        /// <summary>
        /// Send a ping to the server
        /// </summary>
        public void SendPing()
        {
            string id = TicketCounter.MakeId("ping_");
            var child = new ProtocolTreeNode("ping", new[] { new KeyValue("xmlns", "w:p") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get") }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Send a pong to a specific user
        /// </summary>
        /// <param name="id"></param>
        public void SendPong(string id)
        {
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("type", "result"), new KeyValue("to", this.whatsAppRealm), new KeyValue("id", id) });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Send a subscription request
        /// </summary>
        /// <param name="to"></param>
        public void SendPresenceSubscriptionRequest(string to)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "subscribe"), new KeyValue("to", to) });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Request to retrieve the LastOnline string
        /// </summary>
        /// <param name="jid">The jabber id</param>
        public void SendQueryLastOnline(string jid)
        {
            string id = TicketCounter.MakeId("last_");
            var child = new ProtocolTreeNode("query", null);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", jid), new KeyValue("xmlns", "jabber:iq:last") }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Tell the server wether the platform is replay capable
        /// </summary>
        /// <param name="platform">The platform</param>
        /// <param name="value">Capable or not</param>
        public void SendRelayCapable(string platform, bool value)
        {
            string v = TicketCounter.MakeId("relay_");
            var child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push"), new KeyValue("platform", platform), new KeyValue("relay", value ? "1" : "0") });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", v), new KeyValue("type", "set"), new KeyValue("to", this.whatsAppRealm) }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Tell the server the relay was complete
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="millis">Miliseconds it took to relay the message</param>
        public void SendRelayComplete(string id, int millis)
        {
            var child = new ProtocolTreeNode("relay", new[] { new KeyValue("elapsed", millis.ToString(CultureInfo.InvariantCulture)) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("xmlns", "w:p:r"), new KeyValue("type", "result"), new KeyValue("to", "s.whatsapp.net"), new KeyValue("id", id) }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Send a relay timeout
        /// </summary>
        /// <param name="id">The id</param>
        public void SendRelayTimeout(string id)
        {
            var innerChild = new ProtocolTreeNode("remote-server-timeout", new[] { new KeyValue("xmlns", "urn:ietf:params:xml:ns:xmpp-stanzas") });
            var child = new ProtocolTreeNode("error", new[] { new KeyValue("code", "504"), new KeyValue("type", "wait") }, innerChild);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("xmlns", "w:p:r"), new KeyValue("type", "error"), new KeyValue("to", "s.whatsapp.net"), new KeyValue("id", id) }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Request to remove participants from a group
        /// </summary>
        /// <param name="gjid">The group jabber id</param>
        /// <param name="participants">A list of participants</param>
        public void SendRemoveParticipants(string gjid, List<string> participants)
        {
            this.SendRemoveParticipants(gjid, participants, null, null);
        }

        /// <summary>
        /// Request to remove participants from a group
        /// </summary>
        /// <param name="gjid">The group jabber id</param>
        /// <param name="participants">A list of participants</param>
        /// <param name="onSuccess">Action to execute when the request was successful</param>
        /// <param name="onError">Action to execute when the request failed</param>
        public void SendRemoveParticipants(string gjid, List<string> participants, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("remove_group_participants_");
            this.SendVerbParticipants(gjid, participants, id, "remove");
        }

        /// <summary>
        /// Request to set the group subject
        /// </summary>
        /// <param name="gjid">The group jabber id</param>
        /// <param name="subject">The new group subject</param>
        public void SendSetGroupSubject(string gjid, string subject)
        {
            this.SendSetGroupSubject(gjid, subject, null, null);
        }

        /// <summary>
        /// Request to set the group subject
        /// </summary>
        /// <param name="gjid">The group jabber id</param>
        /// <param name="subject">The new group subject</param>
        /// <param name="onSuccess">Action to execute when the request was successful</param>
        /// <param name="onError">Action to execute when the request failed</param>
        public void SendSetGroupSubject(string gjid, string subject, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("set_group_subject_");
            var child = new ProtocolTreeNode("subject", new[] { new KeyValue("value", subject) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", gjid) }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Change the profile picture
        /// </summary>
        /// <param name="jid">The user jabber id</param>
        /// <param name="bytes">The ammount of bytes needed for the photo</param>
        /// <param name="thumbnailBytes">The amount of bytes needed for the thumbanil</param>
        /// <param name="onSuccess">Action to execute when the request was successful</param>
        /// <param name="onError">Action to execute when the request failed</param>
        public void SendSetPhoto(string jid, byte[] bytes, byte[] thumbnailBytes)
        {
            string id = TicketCounter.MakeId("set_photo_");
            var list = new List<ProtocolTreeNode> { new ProtocolTreeNode("picture", null, null, bytes) };
            if (thumbnailBytes != null)
            {
                list.Add(new ProtocolTreeNode("picture", new[] { new KeyValue("type", "preview") }, null, thumbnailBytes));
            }
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:profile:picture"), new KeyValue("to", jid) }, list.ToArray());
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Set the list of people that have been blocked
        /// </summary>
        /// <param name="list">The list of people that have been blocked.</param>
        public void SendSetPrivacyBlockedList(IEnumerable<string> list)
        {
            this.SendSetPrivacyBlockedList(list, null, null);
        }

        /// <summary>
        /// Set the list of people that have been blocked
        /// </summary>
        /// <param name="jidSet">List of jabber id's</param>
        /// <param name="onSuccess">Action to execute when the request was successful</param>
        /// <param name="onError">Action to execute when the request failed</param>
        public void SendSetPrivacyBlockedList(IEnumerable<string> jidSet, Action onSuccess, Action<int> onError)
        {
            string id = TicketCounter.MakeId("privacy_");
            ProtocolTreeNode[] nodeArray = Enumerable.Select<string, ProtocolTreeNode>(jidSet, (Func<string, int, ProtocolTreeNode>)((jid, index) => new ProtocolTreeNode("item", new KeyValue[] { new KeyValue("type", "jid"), new KeyValue("value", jid), new KeyValue("action", "deny"), new KeyValue("order", index.ToString(CultureInfo.InvariantCulture)) }))).ToArray<ProtocolTreeNode>();
            var child = new ProtocolTreeNode("list", new KeyValue[] { new KeyValue("name", "default") }, (nodeArray.Length == 0) ? null : nodeArray);
            var node2 = new ProtocolTreeNode("query", new KeyValue[] { new KeyValue("xmlns", "jabber:iq:privacy") }, child);
            var node3 = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "set") }, node2);
            this.whatsNetwork.SendData(this.BinWriter.Write(node3));
        }

        /// <summary>
        /// Send a status update
        /// </summary>
        /// <param name="status">The status</param>
        /// <param name="onSuccess">Action to execute when the request was successful</param>
        /// <param name="onError">Action to execute when the request failed</param>
        public void SendStatusUpdate(string status, Action onComplete, Action<int> onError)
        {
            string id = TicketManager.GenerateId();
            FMessage message = new FMessage(new FMessage.FMessageIdentifierKey("s.us", true, id));
            var messageNode = GetMessageNode(message, new ProtocolTreeNode("body", null, WhatsApp.SYSEncoding.GetBytes(status)));
            this.whatsNetwork.SendData(this.BinWriter.Write(messageNode));
        }

        /// <summary>
        /// Tell the server the new subject has been recieved
        /// </summary>
        /// <param name="to">The recipient</param>
        /// <param name="id">The id</param>
        public void SendSubjectReceived(string to, string id)
        {
            var child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            var node = GetSubjectMessage(to, id, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Unsubscibe him
        /// </summary>
        /// <param name="jid">The jabber id</param>
        public void SendUnsubscribeHim(string jid)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unsubscribed"), new KeyValue("to", jid) });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Unsubscribe me
        /// </summary>
        /// <param name="jid">The jabber id</param>
        public void SendUnsubscribeMe(string jid)
        {
            var node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unsubscribe"), new KeyValue("to", jid) });
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Tell the server the 'visible' status has been acknowledged
        /// </summary>
        /// <param name="to">Recipient</param>
        /// <param name="id">The id</param>
        public void SendVisibleReceiptAck(string to, string id)
        {
            this.SendReceiptAck(to, id, "visible");
        }

        /// <summary>
        ///  Request to retrieve all groups
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="type">The type</param>
        internal void SendGetGroups(string id, string type)
        {
            var child = new ProtocolTreeNode("list", new[] { new KeyValue("type", type) });
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g"), new KeyValue("to", "g.us") }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Send a message with a body (Plain text);
        /// </summary>
        /// <param name="message">An instance of the FMessage class.</param>
        internal void SendMessageWithBody(FMessage message, bool hidden = false)
        {
            var child = new ProtocolTreeNode("body", null, null, WhatsApp.SYSEncoding.GetBytes(message.data));
            this.whatsNetwork.SendData(this.BinWriter.Write(GetMessageNode(message, child, hidden)));
        }

        /// <summary>
        /// Send a message with media (photo/sound/movie)
        /// </summary>
        /// <param name="message">An instance of the FMessage class.</param>
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
            this.whatsNetwork.SendData(this.BinWriter.Write(GetMessageNode(message, node)));
        }

        public void SendNode(ProtocolTreeNode node)
        {
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Send a verb of group participants
        /// </summary>
        /// <param name="gjid">The group jabber id</param>
        /// <param name="participants">List of participants</param>
        /// <param name="id">The id</param>
        /// <param name="inner_tag">Inner tag</param>
        internal void SendVerbParticipants(string gjid, IEnumerable<string> participants, string id, string inner_tag)
        {
            IEnumerable<ProtocolTreeNode> source = from jid in participants select new ProtocolTreeNode("participant", new[] { new KeyValue("jid", jid) });
            var child = new ProtocolTreeNode(inner_tag, null, source);
            var node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g"), new KeyValue("to", gjid) }, child);
            this.whatsNetwork.SendData(this.BinWriter.Write(node));
        }

        /// <summary>
        /// Processes group settings
        /// </summary>
        /// <param name="groups">A list of instances of the GroupSetting class.</param>
        /// <returns>A list of ProtocolTreeNodes</returns>
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

        /// <summary>
        /// Tell the server the reciepient has been acknowledged
        /// </summary>
        /// <param name="to">The reciepient</param>
        /// <param name="id">The id</param>
        /// <param name="receiptType">The receipt type</param>
        private void SendReceiptAck(string to, string id, string receiptType)
        {
            var tmpChild = new ProtocolTreeNode("ack", new[] { new KeyValue("xmlns", "urn:xmpp:receipts"), new KeyValue("type", receiptType) });
            var resultNode = new ProtocolTreeNode("message", new[]
                                                             {
                                                                 new KeyValue("to", to),
                                                                 new KeyValue("type", "chat"),
                                                                 new KeyValue("id", id)
                                                             }, tmpChild);
            this.whatsNetwork.SendData(this.BinWriter.Write(resultNode));
        }

        /// <summary>
        /// Get the message node
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="pNode">The protocol tree node</param>
        /// <returns>An instance of the ProtocolTreeNode class.</returns>
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

        /// <summary>
        /// Get the message node
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="pNode">The protocol tree node</param>
        /// <returns>An instance of the ProtocolTreeNode class.</returns>
        public static ProtocolTreeNode GetSubjectMessage(string to, string id, ProtocolTreeNode child)
        {
            return new ProtocolTreeNode("message", new[] { new KeyValue("to", to), new KeyValue("type", "subject"), new KeyValue("id", id) }, child);
        }
    }
}