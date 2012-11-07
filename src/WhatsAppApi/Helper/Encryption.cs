using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WhatsAppApi.Helper
{
    static class Encryption
    {
        public static byte[] WhatsappEncrypt(byte[] key, byte[] data, bool appendHash)
        {
            RC4 encryption = new RC4(key, 256);
            HMACSHA1 h = new HMACSHA1(key);
            byte[] buff = new byte[data.Length];
            Buffer.BlockCopy(data, 0, buff, 0, data.Length);

            encryption.Cipher(buff);
            byte[] hashByte = h.ComputeHash(buff);
            byte[] response = new byte[4 + buff.Length];
            if (appendHash)
            {
                Buffer.BlockCopy(buff, 0, response, 0, buff.Length);
                Buffer.BlockCopy(hashByte, 0, response, buff.Length, 4);
            }
            else
            {
                Buffer.BlockCopy(hashByte, 0, response, 0, 4);
                Buffer.BlockCopy(buff, 0, response, 4, buff.Length);
            }

            return response;
        }
        public static byte[] WhatsappDecrypt(byte[] key, byte[] data)
        {
            RC4 encryption = new RC4(key, 256);
            byte[] buff = new byte[data.Length];
            Buffer.BlockCopy(data, 0, buff, 0, data.Length);
            encryption.Cipher(buff);
            return buff;
        }
    }
}
