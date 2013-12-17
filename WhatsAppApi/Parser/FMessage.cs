using System;
using WhatsAppApi.Helper;

namespace WhatsAppApi.Parser
{
    public class FMessage
    {
        public bool gap_behind;
        public FMessageIdentifierKey identifier_key;
        public double latitude;
        public string location_details;
        public string location_url;
        public double longitude;
        public int media_duration_seconds;
        public string media_mime_type;
        public string media_name;
        public long media_size;
        public string media_url;
        public Type media_wa_type;
        public bool offline;
        public string remote_resource;
        public Status status;
        public object thumb_image;
        public DateTime? timestamp;
        public bool wants_receipt;

        public WhatsAppApi.Account.WhatsUser User { get; private set; }

        public FMessage(FMessageIdentifierKey key)
        {
            this.status = Status.Undefined;
            this.gap_behind = true;
            this.identifier_key = key;
        }

        internal FMessage(WhatsAppApi.Account.WhatsUser remote_user, bool from_me)
        {
            this.status = Status.Undefined;
            this.gap_behind = true;
            this.User = remote_user;
            this.identifier_key = new FMessageIdentifierKey(remote_user.GetFullJid(), from_me, TicketManager.GenerateId());
        }
        internal FMessage(string remote_jid, bool from_me)
        {
            this.status = Status.Undefined;
            this.gap_behind = true;
            this.identifier_key = new FMessageIdentifierKey(remote_jid, from_me, TicketManager.GenerateId());
        }

        public FMessage(string remote_jid, string data, object image)
            : this(remote_jid, true)
        {
            this.data = data;
            this.thumb_image = image;
            this.timestamp = new DateTime?(DateTime.Now);
        }
        public FMessage(WhatsAppApi.Account.WhatsUser remote_user, string data, object image)
            : this(remote_user, true)
        {
            this.data = data;
            this.thumb_image = image;
            this.timestamp = new DateTime?(DateTime.Now);
        }

        public void AcceptVisitor(FMessageVisitor visitor)
        {
            switch (this.media_wa_type)
            {
                case Type.Image:
                    visitor.Image(this);
                    return;

                case Type.Audio:
                    visitor.Audio(this);
                    return;

                case Type.Video:
                    visitor.Video(this);
                    return;

                case Type.Contact:
                    visitor.Contact(this);
                    return;

                case Type.Location:
                    visitor.Location(this);
                    return;

                case Type.System:
                    visitor.System(this);
                    return;
            }
            visitor.Undefined(this);
        }

        public static Type GetMessage_WA_Type(string type)
        {
            if ((type != null) && (type.Length != 0))
            {
                if (type.ToUpper().Equals("system".ToUpper()))
                {
                    return Type.System;
                }
                if (type.ToUpper().Equals("image".ToUpper()))
                {
                    return Type.Image;
                }
                if (type.ToUpper().Equals("audio".ToUpper()))
                {
                    return Type.Audio;
                }
                if (type.ToUpper().Equals("video".ToUpper()))
                {
                    return Type.Video;
                }
                if (type.ToUpper().Equals("vcard".ToUpper()))
                {
                    return Type.Contact;
                }
                if (type.ToUpper().Equals("location".ToUpper()))
                {
                    return Type.Location;
                }
            }
            return Type.Undefined;
        }

        public static string GetMessage_WA_Type_StrValue(Type type)
        {
            if (type != Type.Undefined)
            {
                if (type == Type.System)
                {
                    return "system";
                }
                if (type == Type.Image)
                {
                    return "image";
                }
                if (type == Type.Audio)
                {
                    return "audio";
                }
                if (type == Type.Video)
                {
                    return "video";
                }
                if (type == Type.Contact)
                {
                    return "vcard";
                }
                if (type == Type.Location)
                {
                    return "location";
                }
            }
            return null;
        }

        public byte[] binary_data { get; set; }

        public string data { get; set; }

        public class Builder
        {
            internal byte[] binary_data;
            internal string data;
            internal bool? from_me;
            internal string id;
            internal double? latitude;
            internal string location_details;
            internal string location_url;
            internal double? longitude;
            internal int? media_duration_seconds;
            internal string media_name;
            internal long? media_size;
            internal string media_url;
            internal FMessage.Type? media_wa_type;
            internal FMessage message;
            internal bool? offline;
            internal string remote_jid;
            internal string remote_resource;
            internal string thumb_image;
            internal DateTime? timestamp;
            internal bool? wants_receipt;
            internal string serverNickname;

            public byte[] BinaryData()
            {
                return this.binary_data;
            }

            public FMessage.Builder BinaryData(byte[] data)
            {
                this.binary_data = data;
                return this;
            }

            public FMessage Build()
            {
                if (this.message == null)
                {
                    return null;
                }
                if (((this.remote_jid != null) && this.from_me.HasValue) && (this.id != null))
                {
                    this.message.identifier_key = new FMessage.FMessageIdentifierKey(this.remote_jid, this.from_me.Value, this.id);
                    this.message.identifier_key.serverNickname = this.serverNickname;
                }
                if (this.remote_resource != null)
                {
                    this.message.remote_resource = this.remote_resource;
                }
                if (this.wants_receipt.HasValue)
                {
                    this.message.wants_receipt = this.wants_receipt.Value;
                }
                if (this.data != null)
                {
                    this.message.data = this.data;
                }
                if (this.thumb_image != null)
                {
                    this.message.thumb_image = this.thumb_image;
                }
                if (this.timestamp.HasValue)
                {
                    this.message.timestamp = new DateTime?(this.timestamp.Value);
                }
                if (this.offline.HasValue)
                {
                    this.message.offline = this.offline.Value;
                }
                if (this.media_wa_type.HasValue)
                {
                    this.message.media_wa_type = this.media_wa_type.Value;
                }
                if (this.media_size.HasValue)
                {
                    this.message.media_size = this.media_size.Value;
                }
                if (this.media_duration_seconds.HasValue)
                {
                    this.message.media_duration_seconds = this.media_duration_seconds.Value;
                }
                if (this.media_url != null)
                {
                    this.message.media_url = this.media_url;
                }
                if (this.media_name != null)
                {
                    this.message.media_name = this.media_name;
                }
                if (this.latitude.HasValue)
                {
                    this.message.latitude = this.latitude.Value;
                }
                if (this.longitude.HasValue)
                {
                    this.message.longitude = this.longitude.Value;
                }
                if (this.location_url != null)
                {
                    this.message.location_url = this.location_url;
                }
                if (this.location_details != null)
                {
                    this.message.location_details = this.location_details;
                }
                if (this.binary_data != null)
                {
                    this.message.binary_data = this.binary_data;
                }
                return this.message;
            }

            public string Data()
            {
                return this.data;
            }

            public FMessage.Builder Data(string data)
            {
                this.data = data;
                return this;
            }

            public bool? From_me()
            {
                return this.from_me;
            }

            public FMessage.Builder From_me(bool from_me)
            {
                this.from_me = new bool?(from_me);
                return this;
            }

            public string Id()
            {
                return this.id;
            }

            public FMessage.Builder Id(string id)
            {
                this.id = id;
                return this;
            }

            public bool Instantiated()
            {
                return (this.message != null);
            }

            public FMessage.Builder Key(FMessage.FMessageIdentifierKey key)
            {
                this.remote_jid = key.remote_jid;
                this.from_me = new bool?(key.from_me);
                this.id = key.id;
                return this;
            }

            public double? Latitude()
            {
                return this.latitude;
            }

            public FMessage.Builder Latitude(double latitude)
            {
                this.latitude = new double?(latitude);
                return this;
            }

            public string Location_details()
            {
                return this.location_details;
            }

            public FMessage.Builder Location_details(string details)
            {
                this.location_details = details;
                return this;
            }

            public string Location_url()
            {
                return this.location_url;
            }

            public FMessage.Builder Location_url(string url)
            {
                this.location_url = url;
                return this;
            }

            public double? Longitude()
            {
                return this.longitude;
            }

            public FMessage.Builder Longitude(double longitude)
            {
                this.longitude = new double?(longitude);
                return this;
            }

            public int? Media_duration_seconds()
            {
                return this.media_duration_seconds;
            }

            public FMessage.Builder Media_duration_seconds(int media_duration_seconds)
            {
                this.media_duration_seconds = new int?(media_duration_seconds);
                return this;
            }

            public string Media_name()
            {
                return this.media_name;
            }

            public FMessage.Builder Media_name(string media_name)
            {
                this.media_name = media_name;
                return this;
            }

            public long? Media_size()
            {
                return this.media_size;
            }

            public FMessage.Builder Media_size(long media_size)
            {
                this.media_size = new long?(media_size);
                return this;
            }

            public string Media_url()
            {
                return this.media_url;
            }

            public FMessage.Builder Media_url(string media_url)
            {
                this.media_url = media_url;
                return this;
            }

            public FMessage.Type? Media_wa_type()
            {
                return this.media_wa_type;
            }

            public FMessage.Builder Media_wa_type(FMessage.Type media_wa_type)
            {
                this.media_wa_type = new FMessage.Type?(media_wa_type);
                return this;
            }

            public FMessage.Builder NewIncomingInstance()
            {
                if (((this.remote_jid == null) || !this.from_me.HasValue) || (this.id == null))
                {
                    throw new NotSupportedException(
                        "missing required property before instantiating new incoming message");
                }
                this.message =
                    new FMessage(new FMessage.FMessageIdentifierKey(this.remote_jid, this.from_me.Value, this.id));
                return this;
            }

            public FMessage.Builder NewOutgoingInstance()
            {
                if (((this.remote_jid == null) || (this.data == null)) || (this.thumb_image == null))
                {
                    throw new NotSupportedException(
                        "missing required property before instantiating new outgoing message");
                }
                if ((this.id != null) || (this.from_me.Value && !this.from_me.Value))
                {
                    throw new NotSupportedException("invalid property set before instantiating new outgoing message");
                }
                this.message = new FMessage(this.remote_jid, this.data, this.thumb_image);
                return this;
            }

            public bool? Offline()
            {
                return this.offline;
            }

            public FMessage.Builder Offline(bool offline)
            {
                this.offline = new bool?(offline);
                return this;
            }

            public string Remote_jid()
            {
                return this.remote_jid;
            }

            public FMessage.Builder Remote_jid(string remote_jid)
            {
                this.remote_jid = remote_jid;
                return this;
            }

            public string Remote_resource()
            {
                return this.remote_resource;
            }

            public FMessage.Builder Remote_resource(string remote_resource)
            {
                this.remote_resource = remote_resource;
                return this;
            }

            public FMessage.Builder SetInstance(FMessage message)
            {
                this.message = message;
                return this;
            }

            public string Thumb_image()
            {
                return this.thumb_image;
            }

            public FMessage.Builder Thumb_image(string thumb_image)
            {
                this.thumb_image = thumb_image;
                return this;
            }

            public DateTime? Timestamp()
            {
                return this.timestamp;
            }

            public FMessage.Builder Timestamp(DateTime? timestamp)
            {
                this.timestamp = timestamp;
                return this;
            }

            public bool? Wants_receipt()
            {
                return this.wants_receipt;
            }

            public FMessage.Builder Wants_receipt(bool wants_receipt)
            {
                this.wants_receipt = new bool?(wants_receipt);
                return this;
            }
        }

        public class FMessageIdentifierKey
        {
            public bool from_me;
            public string id;
            public string remote_jid;
            public string serverNickname;

            public FMessageIdentifierKey(string remote_jid, bool from_me, string id)
            {
                this.remote_jid = remote_jid;
                this.from_me = from_me;
                this.id = id;
            }

            public override bool Equals(object obj)
            {
                if (this != obj)
                {
                    if (obj == null)
                    {
                        return false;
                    }
                    if (base.GetType() != obj.GetType())
                    {
                        return false;
                    }
                    FMessage.FMessageIdentifierKey key = (FMessage.FMessageIdentifierKey)obj;
                    if (this.from_me != key.from_me)
                    {
                        return false;
                    }
                    if (this.id == null)
                    {
                        if (key.id != null)
                        {
                            return false;
                        }
                    }
                    else if (!this.id.Equals(key.id))
                    {
                        return false;
                    }
                    if (this.remote_jid == null)
                    {
                        if (key.remote_jid != null)
                        {
                            return false;
                        }
                    }
                    else if (!this.remote_jid.Equals(key.remote_jid))
                    {
                        return false;
                    }
                }
                return true;
            }

            public override int GetHashCode()
            {
                int num = 0x1f;
                int num2 = 1;
                num2 = (0x1f * 1) + (this.from_me ? 0x4cf : 0x4d5);
                num2 = (num * num2) + ((this.id == null) ? 0 : this.id.GetHashCode());
                return ((num * num2) + ((this.remote_jid == null) ? 0 : this.remote_jid.GetHashCode()));
            }

            public override string ToString()
            {
                return
                    string.Concat(new object[]
                                      {
                                          "Key[id=", this.id, ", from_me=", this.from_me, ", remote_jid=", this.remote_jid,
                                          "]"
                                      });
            }
        }

        public enum Status
        {
            UnsentOld,
            Uploading,
            Uploaded,
            SentByClient,
            ReceivedByServer,
            ReceivedByTarget,
            NeverSend,
            ServerBounce,
            Undefined,
            Unsent
        }

        public enum Type
        {
            Audio = 2,
            Contact = 4,
            Image = 1,
            Location = 5,
            System = 7,
            Undefined = 0,
            Video = 3
        }
    }
}