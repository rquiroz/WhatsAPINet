using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Settings
{
    class WhatsConstants
    {
        #region ServerConstants
        public const string WhatsAppDigest = "xmpp/s.whatsapp.net";
        public const string WhatsAppHost = "bin-short.whatsapp.net";
        public const string WhatsAppRealm = "s.whatsapp.net";
        public const string WhatsAppServer = "s.whatsapp.net";
        public const string WhatsGroupChat = "g.us";
        //public const string WhatsAppVer = "2.8.2";
        public const string WhatsAppVer = "2.8.4";
        public const int WhatsPort = 5222;

        public const string IphoneDevice = "iPhone";
        //public const string UserAgend = "WhatsApp/2.8.2 WP7/7.10.8773.98 Device/NOKIA-Lumia_800-H112.1402.2.3"; //"WhatsApp/2.8.2 WP7/2.3.7 Device/HTC-HERO-H1.0";
        public const string UserAgend = "WhatsApp/2.8.4 iPhone_OS/6.0.1 Device/iPhone_4S";
        public const string WhatsBuildHash = "889d4f44e479e6c38b4a834c6d8417815f999abe";//v2.8.0"c0d4db538579a3016902bf699c16d490acf91ff4"; //v2.0.0 "13944fe0a89d1e6cce0c405178f7c0d00313d558";
        #endregion

        #region ParserConstants
        public static NumberStyles WhatsAppNumberStyle = (NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);
        public static DateTime UnixEpoch = new DateTime(0x7b2, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        #endregion
    }
}
