using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WhatsAppApi.Helper
{
    static class Encryption
    {
        public static RC4 encryptionOutgoing = null;
        public static RC4 encryptionIncoming = null;

        public static byte[] WhatsappEncrypt(byte[] key, byte[] data, bool appendHash)
        {
            if(encryptionOutgoing == null)
                encryptionOutgoing = new RC4(key, 256);
            HMACSHA1 h = new HMACSHA1(key);
            byte[] buff = new byte[data.Length];
            Buffer.BlockCopy(data, 0, buff, 0, data.Length);

            encryptionOutgoing.Cipher(buff);
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
            if (encryptionIncoming == null)
                encryptionIncoming = new RC4(key, 256);
            byte[] buff = new byte[data.Length];
            Buffer.BlockCopy(data, 0, buff, 0, data.Length);
            encryptionIncoming.Cipher(buff);
            return buff;
        }
    }
}
