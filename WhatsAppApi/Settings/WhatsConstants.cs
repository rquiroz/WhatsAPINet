using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Settings
{
    /// <summary>
    /// Holds constant information used to connect to whatsapp server
    /// </summary>
    class WhatsConstants
    {
        #region ServerConstants

        /// <summary>
        /// The whatsapp digest
        /// </summary>
        public const string WhatsAppDigest = "xmpp/s.whatsapp.net";

        /// <summary>
        /// The whatsapp host
        /// </summary>
        public const string WhatsAppHost = "c.whatsapp.net";

        /// <summary>
        /// The whatsapp XMPP realm
        /// </summary>
        public const string WhatsAppRealm = "s.whatsapp.net";

        /// <summary>
        /// The whatsapp server
        /// </summary>
        public const string WhatsAppServer = "s.whatsapp.net";

        /// <summary>
        /// The whatsapp group chat server
        /// </summary>
        public const string WhatsGroupChat = "g.us";


        /// <summary>
        /// The whatsapp version the client complies to
        /// </summary>
        public const string WhatsAppVer = "2.11.69";

        /// <summary>
        /// The port that needs to be connected to
        /// </summary>
        public const int WhatsPort = 443;

        /// <summary>
        /// iPhone device
        /// </summary>
        public const string IphoneDevice = "Android";

        /// <summary>
        /// The useragent used for http requests
        /// </summary>
        public const string UserAgent = "WhatsApp/2.11.69 Android/4.2.1 Device/GalaxyS3";

        #endregion

        #region ParserConstants
        /// <summary>
        /// The number style used
        /// </summary>
        public static NumberStyles WhatsAppNumberStyle = (NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);

        /// <summary>
        /// Unix epoch DateTime 
        /// </summary>
        public static DateTime UnixEpoch = new DateTime(0x7b2, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        #endregion
    }
}
