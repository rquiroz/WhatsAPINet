using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WhatsAppApi
{
    public class ApiBase
    {
        public enum CONNECTION_STATUS
        {
            UNAUTHORIZED,
            DISCONNECTED,
            CONNECTED,
            LOGGEDIN
        }

        public enum ImageType
        {
            JPEG,
            GIF,
            PNG
        }

        public enum VideoType
        {
            MOV,
            AVI,
            MP4
        }

        public enum AudioType
        {
            WAV,
            OGG,
            MP3
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
                case VisibilityCategory.Status:
                    return "status";
                case VisibilityCategory.ProfilePhoto:
                    return "profile";
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
                case "profile":
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

        protected byte[] CreateThumbnail(byte[] imageData)
        {
            Image image;
            using (System.IO.MemoryStream m = new System.IO.MemoryStream(imageData))
            {
                image = Image.FromStream(m);
            }
            if (image != null)
            {
                int newHeight = 0;
                int newWidth = 0;
                float imgWidth = float.Parse(image.Width.ToString());
                float imgHeight = float.Parse(image.Height.ToString());
                if (image.Width > image.Height)
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
                    gr.DrawImage(image, new Rectangle(0, 0, newWidth, newHeight));
                }
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                ms.Close();
                return ms.ToArray();
            }
            return null;
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
    }
}
