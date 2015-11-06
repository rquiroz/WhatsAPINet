using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WhatsAppApi.Register
{
    class WaToken
    {
        public static string GenerateToken(string number)
        {

            string token = "PdA2DJyKoUrwLw1Bg6EIhzh502dF9noR9uFCllGk1439921717185" + number;
            byte[] asciiBytes = ASCIIEncoding.ASCII.GetBytes(token);
            byte[] hashedBytes = MD5CryptoServiceProvider.Create().ComputeHash(asciiBytes);
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

        }

        private static List<byte> GetFilledList(byte item, int length)
        {
            List<byte> result = new List<byte>();
            for (int i = 0; i < length; i++)
            {
                result.Add(item);
            }
            return result;
        }
    }
}
