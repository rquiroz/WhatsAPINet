using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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

        protected static DateTime GetDateTimeFromTimestamp(string timestamp)
        {
            long data = 0;
            if (long.TryParse(timestamp, out data))
            {
                return GetDateTimeFromTimestamp(data);
            }
            return DateTime.Now;
        }

        protected static DateTime GetDateTimeFromTimestamp(long timestamp)
        {
            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return UnixEpoch.AddSeconds(timestamp);
        }

        protected byte[] ProcessProfilePicture(byte[] bytes)
        {
            Bitmap image;
            using (System.IO.MemoryStream m = new System.IO.MemoryStream(bytes))
            {
                image = new Bitmap(Image.FromStream(m));
            }
            if (image != null)
            {
                int size = 640;
                if (size > image.Width)
                    size = image.Width;
                if (size > image.Height)
                    size = image.Height;

                int newHeight = 0;
                int newWidth = 0;
                float imgWidth = float.Parse(image.Width.ToString());
                float imgHeight = float.Parse(image.Height.ToString());
                if (image.Width < image.Height)
                {
                    newHeight = (int)((imgHeight / imgWidth) * size);
                    newWidth = size;
                }
                else
                {
                    newWidth = (int)((imgWidth / imgHeight) * size);
                    newHeight = size;
                }

                Bitmap newImage = new Bitmap(newWidth, newHeight);
                using (Graphics gr = Graphics.FromImage(newImage))
                {
                    gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    gr.DrawImage(image, new Rectangle(0, 0, newWidth, newHeight));
                }

                //crop square
                Bitmap dest = newImage.Clone(new Rectangle(
                    new Point(0, 0),
                    new Size(size, size)
                    ), image.PixelFormat);

                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                System.Drawing.Imaging.Encoder enc = System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters encParams = new EncoderParameters(1);

                EncoderParameter param = new EncoderParameter(enc, 50L);
                encParams.Param[0] = param;
                dest.Save(ms, GetEncoder(ImageFormat.Jpeg), encParams);
                ms.Close();
                return ms.ToArray();
            }
            return bytes;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
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
            target = target.TrimStart(new char[] { '+', '0' });
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
