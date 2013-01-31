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
        public const string WhatsAppVer = "2.8.7";
        public const int WhatsPort = 5222;

        public const string IphoneDevice = "iPhone";
        public const string UserAgend = "WhatsApp/2.8.7 iPhone_OS/6.1.0 Device/iPhone_4S";
        public const string WhatsBuildHash = "889d4f44e479e6c38b4a834c6d8417815f999abe";
        #endregion

        #region ParserConstants
        public static NumberStyles WhatsAppNumberStyle = (NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);
        public static DateTime UnixEpoch = new DateTime(0x7b2, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        #endregion
    }
}
