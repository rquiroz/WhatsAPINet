using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WhatsAppApi.Settings;

namespace WhatsAppApi.Register
{
    public static class WhatsRegister
    {
        public static bool RegisterUser(string countryCode, string phoneNumber)
        {
            string website = "https://r.whatsapp.net/v1/code.php";
            string postData = GetRegString(countryCode, phoneNumber);
            string both = website + "?" + postData;

            var result = StartWebRequest("", "", WhatsConstants.UserAgend, both);
            Console.WriteLine(result);
            return result.Contains("status=\"success-sent\"");
            /*
             * <code>
             * <response status="success-sent" result="60"/>
             * </code>
             */
        }

        public static bool VerifyRegistration(string countryCode, string phoneNumber, string password, string code)
        {
            string tmpPassword = password.ToPassword();
            string verifyString = string.Format("https://r.whatsapp.net/v1/register.php?cc={0}&in={1}&udid={2}&code={3}", new object[] { countryCode, phoneNumber, tmpPassword, code });

            var result = StartWebRequest("", "", WhatsConstants.UserAgend, verifyString);
            Console.WriteLine(result);
            return true;

            /*
             * <register>
             * <response status="ok" login="phoneNumber" result="new"/>
             * </register>
             * 
             */
        }

        public static bool ExistsAndDelete(string countrycode, string phone, string pass)
        {
            string webString = string.Format("https://r.whatsapp.net/v1/exist.php?cc={0}&in={1}", System.Uri.EscapeDataString(countrycode), System.Uri.EscapeDataString(phone));
            if (pass != null)
            {
                webString = webString + string.Format("&udid={0}", pass.ToPassword());
            }

            var result = StartWebRequest("", "", WhatsConstants.UserAgend, webString);
            return result.Contains("status=\"ok\"");
        }

        private static string StartWebRequest(string website, string postData, string userAgent, string both)
        {
            var request = (HttpWebRequest)WebRequest.Create(both);
            request.UserAgent = userAgent;
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var html = reader.ReadToEnd();
                    return html;
                }
            }
            catch (WebException ex)
            {
                return "error";
            }
        }

        private static string MD5String(this string pass)
        {
            MD5 md5 = MD5.Create();
            byte[] dataMd5 = md5.ComputeHash(Encoding.UTF8.GetBytes(pass));
            return ByteToString(dataMd5);
        }

        private static string ByteToString(byte[] dataMd5)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < dataMd5.Length; i++)
                sb.AppendFormat("{0:x2}", dataMd5[i]);
            return sb.ToString();
        }

        private static string GetRegString(string countryCode, string phonenum, string codeType = "sms")
        {
            string tmpLangCode;
            string tmpLocalCode;
            GetLangAndLocale(CultureInfo.CurrentCulture, out tmpLangCode, out tmpLocalCode);
            if (tmpLocalCode == "029")
            {
                tmpLocalCode = "US";
            }
            //string countryCode = "49";
            string phoneNumber = phonenum;
            const string buildHash = WhatsConstants.WhatsBuildHash;
            string tmpToken = ("k7Iy3bWARdNeSL8gYgY6WveX12A1g4uTNXrRzt1H" + buildHash + phoneNumber).MD5String().ToLower();
            string regString = string.Format("cc={0}&in={1}&lg={2}&lc={3}&method={4}&mcc=000&mnc=000&imsi=000&token={5}", new object[] { countryCode, phoneNumber, tmpLangCode, tmpLocalCode, codeType, tmpToken });
            return regString;
        }

        private static string ToPassword(this string bs)
        {
            return (new string(bs.Reverse().ToArray())).MD5String();
        }

        private static void GetLangAndLocale(CultureInfo that, out string lang, out string locale)
        {
            string name = that.Name;
            int index = name.IndexOf('-');
            if (index > 0)
            {
                int num2 = name.LastIndexOf('-');
                lang = name.Substring(0, index);
                locale = name.Substring(num2 + 1);
            }
            else
            {
                lang = name;
                switch (lang)
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
                locale = lang.ToUpper();
            }
        }

    }
}
