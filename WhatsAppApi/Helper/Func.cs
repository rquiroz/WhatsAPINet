using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    class Func
    {
        public static bool isShort(string value)
        {
            return value.Length < 256;
        }

        public static int strlen_wa(string str)
        {
            int len = str.Length;
            if (len >= 256)
                len = len & 0xFF00 >> 8;
            return len;
        }

        public static string _hex(int val)
        {
            return (val.ToString("X").Length%2 == 0) ? val.ToString("X") : ("0" + val.ToString("X"));
        }

        public static string random_uuid()
        {
            var mt_rand = new Random();
            return string.Format("{0}{1}-{2}-{3}-{4}-{5}{6}{7}",
                                 mt_rand.Next(0, 0xffff), mt_rand.Next(0, 0xffff),
                                 mt_rand.Next(0, 0xffff),
                                 mt_rand.Next(0, 0x0fff) | 0x4000,
                                 mt_rand.Next(0, 0x3fff) | 0x8000,
                                 mt_rand.Next(0, 0xffff), mt_rand.Next(0, 0xffff), mt_rand.Next(0, 0xffff)
                );
        }

        public static string strtohex(string str)
        {
            string hex = "0x";
            for (int i = 0; i < str.Length; i++)
                hex += ((int) str[i]).ToString("x");
            return hex;
        }

        public static string HexString2Ascii(string hexString)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= hexString.Length - 2; i += 2)
            {
                sb.Append(Convert.ToString(Convert.ToChar(Int32.Parse(hexString.Substring(i, 2), System.Globalization.NumberStyles.HexNumber))));
            }
            return sb.ToString();
        }

        public static long GetUnixTimestamp(DateTime value)
        {
            TimeSpan span = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            return (long)span.TotalSeconds;
        }

        public static long GetNowUnixTimestamp()
        {
            return GetUnixTimestamp(DateTime.UtcNow);
        }

        public static bool ArrayEqual(byte[] b1, byte[] b2)
        {
            int len = b1.Length;
            if (b1.Length != b2.Length)
                return false;
            for (int i = 0; i < len; i++)
            {
                if (b1[i] != b2[i])
                    return false;
            }
            return true;
        }
    }
}
