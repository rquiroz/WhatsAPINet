using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace WhatsAppApi.Helper
{
    public class ContactSync
    {
        const string RequestURL = "https://sro.whatsapp.net/v2/sync/a";
        const string ExecureURL = "https://sro.whatsapp.net/v2/sync/q";
        protected string username;
        protected string password;
        protected HttpWebRequest request;

        public ContactSync(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        public ContactSyncResult[] Sync(string[] contacts)
        {
            string nonce = this._getSyncNonce();
            string res = this._executeSync(nonce, contacts);

            JavaScriptSerializer jss = new JavaScriptSerializer();
            ContactSyncResultContainer c = jss.Deserialize<ContactSyncResultContainer>(res);

            return c.c;
        }

        protected string _getPostfields(string[] contacts)
        {
            string fields = "ut=all&t=c";
            foreach (string contact in contacts)
            {
                string con = contact;
                if (!con.Contains('+'))
                {
                    con = "%2B" + con;
                }
                fields += "&u[]=" + con;
            }
            return fields;
        }

        protected string _executeSync(string cnonce, string[] contacts)
        {
            this.request = WebRequest.Create(ExecureURL) as HttpWebRequest;
            string postfields = this._getPostfields(contacts);
            request.Method = "POST";
            this._setHeaders(cnonce, postfields.Length);
            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(postfields);
            }
            try
            {
                HttpWebResponse response = this.request.GetResponse() as HttpWebResponse;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                return reader.ReadToEnd();
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected string _getSyncNonce()
        {
            this.request = WebRequest.Create(RequestURL) as HttpWebRequest;
            this._setHeaders("0", 0);
            HttpWebResponse response = this.request.GetResponse() as HttpWebResponse;
            string cnonce = this._getCnonce(response.Headers.Get("WWW-Authenticate"));
            return cnonce;
        }

        protected string _getCnonce(string header)
        {
            string[] parts = header.Split(',');
            parts = parts.Last().Replace('\\', '\0').Split('"');
            return parts[1];
        }

        protected static string _getCnonce()
        {
            string foo = _hash(DateTime.Now.Ticks.ToString()).Substring(0, 10);//random
            return _hash(foo);
        }

        protected static string _hash(string data)
        {
            return _hash(data, false);
        }

        protected static string _hash(string data, bool raw)
        {
            byte[] bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(data);
            MD5 md5 = MD5.Create();
            md5.ComputeHash(bytes);
            if (!raw)
            {
                return _hexEncode(md5.Hash);
            }
            else
            {
                return Encoding.GetEncoding("ISO-8859-1").GetString(md5.Hash);
            }
        }

        protected static string _hexEncode(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }

        protected void _setHeaders(string nonce, long contentlength)
        {
            this.request.Headers.Clear();
            this.request.UserAgent = WhatsAppApi.Settings.WhatsConstants.UserAgent;
            this.request.Accept = "text/json";
            this.request.ContentType = "application/x-www-form-urlencoded";
            string foo = this._generateAuth(nonce);
            this.request.Headers.Add("Authorization", foo);
            this.request.Headers.Add("Accept-Encoding", "identity");
            this.request.ContentLength = contentlength;
        }

        protected string _generateAuth(string nonce)
        {
            string cnonce = _getCnonce();
            string nc = "00000001";
            string digestUri = "WAWA/s.whatsapp.net";
            string credentials = this.username + ":s.whatsapp.net:";
            credentials += Encoding.GetEncoding("ISO-8859-1").GetString(Convert.FromBase64String(this.password));
            string response = _hash(_hash(_hash(credentials, true) + ":" + nonce + ":" + cnonce) + ":" + nonce + ":" + nc + ":" + cnonce + ":auth:" + _hash("AUTHENTICATE:" + digestUri));
            return "X-WAWA:username=\"" + this.username + "\",realm=\"s.whatsapp.net\",nonce=\"" + nonce + "\",cnonce=\"" + cnonce + "\",nc=\"" + nc + "\",qop=\"auth\",digest-uri=\"" + digestUri + "\",response=\"" + response + "\",charset=\"utf-8\"";
        }
    }

    public class ContactSyncResult
    {
        public string p { get; set; }
        public string n { get; set; }
        public string s { get; set; }
        public long t { get; set; }
        public int w { get; set; }
    }

    public class ContactSyncResultContainer
    {
        public ContactSyncResult[] c { get; set; }
    }
}