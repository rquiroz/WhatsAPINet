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
        public static bool RequestCode(string countryCode, string phoneNumber, string method = "sms")
        {
            try
            {
                string language, locale;
                CultureInfo.CurrentCulture.GetLanguageAndLocale(out language, out locale);
                string id = phoneNumber.Reverse().ToMD5String();
                string token = string.Concat(WhatsConstants.WhatsBuildHash, phoneNumber).ToMD5String();
                string uri = string.Format("https://v.whatsapp.net/v2/code?cc={0}&in={1}&to={0}{1}&lg={2}&lc={3}&mcc=000&mnc=000&method={4}&id={5}&token={6}", countryCode, phoneNumber, language, locale, method, id, token);
                return (GetResponse(uri).GetJsonValue("status") == "sent");
            }
            catch
            {
                return false;
            }
        }

        public static string RegisterCode(string countryCode, string phoneNumber, string code)
        {
            try
            {
                string id = phoneNumber.Reverse().ToMD5String();
                string uri = string.Format("https://v.whatsapp.net/v2/register?cc={0}&in={1}&id={2}&code={3}", countryCode, phoneNumber, id, code);
                if (GetResponse(uri).GetJsonValue("status") == "ok")
                {
                    return GetResponse(uri).GetJsonValue("pw");
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
            var request = HttpWebRequest.CreateHttp(new Uri(uri));
            request.KeepAlive = false;
            request.Date = DateTime.Now;
            request.UserAgent = WhatsConstants.UserAgent;
            request.Accept = "text/json";
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadLine();
            }
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
