using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Settings;

namespace WhatsAppApi
{
    public class WhatsAppBase : WhatsEventBase
    {
        public enum CONNECTION_STATUS
        {
            UNAUTHORIZED,
            DISCONNECTED,
            CONNECTED,
            LOGGEDIN
        }

        public enum VisibilityCategory
        {
            ProfilePhoto,
            Status,
            LastSeenTime
        }

        public enum VisibilitySetting
        {
            None,
            Contacts,
            Everyone
        }

        protected string privacySettingToString(VisibilitySetting s)
        {
            switch (s)
            {
                case VisibilitySetting.None:
                    return "none";
                case VisibilitySetting.Contacts:
                    return "contacts";
                case VisibilitySetting.Everyone:
                    return "all";
                default:
                    throw new Exception("Invalid visibility setting");
            }
        }

        protected string privacyCategoryToString(VisibilityCategory c)
        {
            switch (c)
            {
                case VisibilityCategory.LastSeenTime:
                    return "last";
                    break;
                case VisibilityCategory.Status:
                    return "status";
                    break;
                case VisibilityCategory.ProfilePhoto:
                    return "photo";
                    break;
                default:
                    throw new Exception("Invalid privacy category");
            }
        }

        protected VisibilityCategory parsePrivacyCategory(string data)
        {
            switch (data)
            {
                case "last":
                    return VisibilityCategory.LastSeenTime;
                case "status":
                    return VisibilityCategory.Status;
                case "photo":
                    return VisibilityCategory.ProfilePhoto;
                default:
                    throw new Exception(String.Format("Could not parse {0} as privacy category", data));
            }
        }

        protected VisibilitySetting parsePrivacySetting(string data)
        {
            switch (data)
            {
                case "none":
                    return VisibilitySetting.None;
                case "contacts":
                    return VisibilitySetting.Contacts;
                case "all":
                    return VisibilitySetting.Everyone;
                default:
                    throw new Exception(string.Format("Cound not parse {0} as privacy setting", data));
            }
        }

        protected ProtocolTreeNode uploadResponse;

        protected AccountInfo accountinfo;

        public static bool DEBUG;

        protected string password;

        protected bool hidden;

        protected CONNECTION_STATUS loginStatus;

        public CONNECTION_STATUS ConnectionStatus
        {
            get
            {
                return this.loginStatus;
            }
        }

        protected KeyStream outputKey;

        protected object messageLock = new object();

        protected List<ProtocolTreeNode> messageQueue;

        protected string name;

        protected string phoneNumber;

        protected BinTreeNodeReader reader;

        protected int timeout = 300000;

        protected WhatsNetwork whatsNetwork;

        public static readonly Encoding SYSEncoding = Encoding.UTF8;

        protected byte[] _challengeBytes;

        protected BinTreeNodeWriter BinWriter;

        protected void _constructBase(string phoneNum, string imei, string nick, bool debug, bool hidden)
        {
            this.messageQueue = new List<ProtocolTreeNode>();
            this.phoneNumber = phoneNum;
            this.password = imei;
            this.name = nick;
            this.hidden = hidden;
            WhatsApp.DEBUG = debug;
            this.reader = new BinTreeNodeReader();
            this.loginStatus = CONNECTION_STATUS.DISCONNECTED;
            this.BinWriter = new BinTreeNodeWriter();
            this.whatsNetwork = new WhatsNetwork(WhatsConstants.WhatsAppHost, WhatsConstants.WhatsPort, this.timeout);
        }

        public void Connect()
        {
            try
            {
                this.whatsNetwork.Connect();
                this.loginStatus = CONNECTION_STATUS.CONNECTED;
                //success
                this.fireOnConnectSuccess();
            }
            catch (Exception e)
            {
                this.fireOnConnectFailed(e);
            }
        }

        public void Disconnect(Exception ex = null)
        {
            this.whatsNetwork.Disconenct();
            this.loginStatus = CONNECTION_STATUS.DISCONNECTED;
            this.fireOnDisconnect(ex);
        }

        protected byte[] encryptPassword()
        {
            return Convert.FromBase64String(this.password);
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

        public void AddMessage(ProtocolTreeNode node)
        {
            lock (messageLock)
            {
                this.messageQueue.Add(node);
            }
        }

        public bool HasMessages()
        {
            if (this.messageQueue == null)
                return false;
            return this.messageQueue.Count > 0;
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

        protected byte[] CreateThumbnail(string path)
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
                    using (Graphics gr = Graphics.FromImage(newImage))
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

        protected void DebugPrint(string debugMsg)
        {
            if (WhatsApp.DEBUG && debugMsg.Length > 0)
            {
                Console.WriteLine(debugMsg);
            }
        }

        protected string md5(string pass)
        {
            MD5 md5 = MD5.Create();
            byte[] dataMd5 = md5.ComputeHash(WhatsApp.SYSEncoding.GetBytes(pass));
            var sb = new StringBuilder();
            for (int i = 0; i < dataMd5.Length; i++)
                sb.AppendFormat("{0:x2}", dataMd5[i]);
            return sb.ToString();
        }

        protected void SendNode(ProtocolTreeNode node)
        {
            this.SendData(this.BinWriter.Write(node));
        }
    }
}
