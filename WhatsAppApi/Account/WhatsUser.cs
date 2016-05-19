using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhatsAppApi.Settings;

namespace WhatsAppApi.Account
{
    public class WhatsUser
    {
        private string serverUrl;
        public string Nickname { get; set; }
        public string Jid { get; private set; }

        public WhatsUser(string jid, string srvUrl, string nickname = "")
        {
            this.Jid = jid;
            this.Nickname = nickname;
            this.serverUrl = srvUrl;
        }

        public string GetFullJid()
        {
            return WhatsAppApi.WhatsApp.GetJID(this.Jid);
        }

        internal void SetServerUrl(string srvUrl)
        {
            this.serverUrl = srvUrl;
        }

        public override string ToString()
        {
            return this.GetFullJid();
        }
    }
}
