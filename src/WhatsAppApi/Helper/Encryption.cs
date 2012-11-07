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
            byte[] buff2 = new byte[data.Length + 4];
            Buffer.BlockCopy(data, 0, buff2, 4, data.Length);

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

        public static void DecodeMessage(byte[] key,byte[] buffer, int macOffset, int offset, int length)
        {
            RC4 rc4 = new RC4(key, 256);
            HMACSHA1 h = new HMACSHA1(key);
            byte[] buffer2 = h.ComputeHash(buffer, offset, length);
            for (int i = 0; i < 4; i++)
            {
                if (buffer2[i] != buffer[macOffset + i])
                {
                    return;
                }
            }
            rc4.Cipher(buffer, offset, length);
            Array.Copy(buffer, offset, buffer, macOffset, length);
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
