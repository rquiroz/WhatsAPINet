using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WhatsAppApi.Settings;

namespace WhatsAppApi.Register
{
    public static class WhatsRegisterV2
    {
        const string TOKEN_URL = "http://mywapi.nl/token";
        public static string GenerateIdentity(string phoneNumber, string salt = "")
        {
            return (phoneNumber + salt).Reverse().ToSHAString();
        }

        private static string GetToken(string number)
        {
            HttpWebRequest request = WebRequest.Create(WhatsRegisterV2.TOKEN_URL) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(String.Format("in={0}", number));
            }
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            string token = null;
            try
            {
            using(StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                token = sr.ReadToEnd();
            }
            } catch(Exception e)
            {
                throw new Exception("Could not request token", e);
            }
            return token;
        }

        public static bool RequestCode(string countryCode, string phoneNumber, out string password, string method = "sms", string id = null, string language = null, string locale = null, string mcc = "204", string salt = "")
        {
            string response = string.Empty;
            return RequestCode(countryCode, phoneNumber, out password, out response, method, id, mcc, salt);
        }

        public static bool RequestCode(string countryCode, string phoneNumber, out string password, out string response, string method = "sms", string id = null, string language = null, string locale = null, string mcc = "204", string salt = "")
        {
            response = null;
            password = null;
            try
            {
                if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(locale))
                {
                    CultureInfo.CurrentCulture.GetLanguageAndLocale(out language, out locale);
                }
                if (string.IsNullOrEmpty(id))
                {
                    //auto-generate
                    id = GenerateIdentity(phoneNumber, salt);
                }
                string token = System.Uri.EscapeDataString(WhatsRegisterV2.GetToken(phoneNumber));
                string uri = string.Format("https://v.whatsapp.net/v2/code?cc={0}&in={1}&to={0}{1}&lg={2}&lc={3}&mcc={7}&mnc=008&method={4}&id={5}&token={6}", countryCode, phoneNumber, language, locale, method, id, token, mcc);
                response = GetResponse(uri);
                password = response.GetJsonValue("pw");
                if (!string.IsNullOrEmpty(password))
                {
                    return true;
                }
                return (response.GetJsonValue("status") == "sent");
            }
            catch
            {
                return false;
            }
        }

        public static string RegisterCode(string countryCode, string phoneNumber, string code, string id = null, string salt = "")
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    //auto generate
                    id = GenerateIdentity(phoneNumber, salt);
                }
                string uri = string.Format("https://v.whatsapp.net/v2/register?cc={0}&in={1}&id={2}&code={3}", countryCode, phoneNumber, id, code);
                string response = GetResponse(uri);
                if (response.GetJsonValue("status") == "ok")
                {
                    return response.GetJsonValue("pw");
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string RequestExist(string countryCode, string phoneNumber, string id = null)
        {
            try
            {
                if (String.IsNullOrEmpty(id))
                {
                    id = phoneNumber.Reverse().ToSHAString();
                }
                string uri = string.Format("https://v.whatsapp.net/v2/exist?cc={0}&in={1}&id={2}", countryCode, phoneNumber, id);
                string response = GetResponse(uri);
                if (response.GetJsonValue("status") == "ok")
                {
                    return response.GetJsonValue("pw");
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string GetResponse(string uri)
        {
            HttpWebRequest request = HttpWebRequest.Create(new Uri(uri)) as HttpWebRequest;
            request.KeepAlive = false;
            request.UserAgent = WhatsConstants.UserAgent;
            request.Accept = "text/json";
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadLine();
            }
        }

        private static string ToSHAString(this IEnumerable<char> s)
        {
            return new string(s.ToArray()).ToSHAString();
        }

        private static string ToSHAString(this string s)
        {
            byte[] data = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(s));
            string str = Encoding.ASCII.GetString(data);
            return System.Uri.EscapeDataString(str).ToLower();
        }

        private static string ToMD5String(this IEnumerable<char> s)
        {
            return new string(s.ToArray()).ToMD5String();
        }
 
        private static string ToMD5String(this string s)
        {
            return string.Join(string.Empty, MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(s)).Select(item => item.ToString("x2")).ToArray());
        }


        private static void GetLanguageAndLocale(this CultureInfo self, out string language, out string locale)
        {
            string name = self.Name;
            int n1 = name.IndexOf('-');
            if (n1 > 0)
            {
                int n2 = name.LastIndexOf('-');
                language = name.Substring(0, n1);
                locale = name.Substring(n2 + 1);
            }
            else
            {
                language = name;
                switch (language)
                {
                    case "cs":
                        locale = "CZ";
                        return;

                    case "da":
                        locale = "DK";
                        return;

                    case "el":
                        locale = "GR";
                        return;

                    case "ja":
                        locale = "JP";
                        return;

                    case "ko":
                        locale = "KR";
                        return;

                    case "sv":
                        locale = "SE";
                        return;

                    case "sr":
                        locale = "RS";
                        return;
                }
                locale = language.ToUpper();
            }
        }

        private static string GetJsonValue(this string s, string parameter)
        {
            Match match;
            if ((match = Regex.Match(s, string.Format("\"?{0}\"?:\"(?<Value>.+?)\"", parameter), RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success)
            {
                return match.Groups["Value"].Value;
            }
            return null;
        }
    }
}
