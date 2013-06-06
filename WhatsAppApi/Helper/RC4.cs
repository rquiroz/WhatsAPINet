using System;

namespace WhatsAppApi.Helper
{
    public class RC4
    {
        private int i = 0;
        private int j = 0;
        private int[] s;

        public RC4(byte[] key, int drop)
        {
            s = new int[256];
            while (this.i < this.s.Length)
            {
                this.s[this.i] = this.i;
                this.i++;
            }
            this.j = 0;
            this.i = 0;
            while (this.i < 0x100)
            {
                this.j = ((this.j + key[this.i % key.Length]) + this.s[this.i]) & 0xff;
                Swap<int>(this.s, this.i, this.j);
                this.i++;
            }
            this.i = this.j = 0;
            this.Cipher(new byte[drop]);
        }

        public void Cipher(byte[] data)
        {
            this.Cipher(data, 0, data.Length);
        }

        public void Cipher(byte[] data, int offset, int length)
        {
            for (int i = length; i > 0; i--)
            {
                this.i = (this.i + 1) & 0xff;
                this.j = (this.j + this.s[this.i]) & 0xff;
                Swap<int>(this.s, this.i, this.j);
                int index = offset++;
                data[index] = (byte)(data[index] ^ this.s[(this.s[this.i] + this.s[this.j]) & 0xff]);
            }
        }

        private static void Swap<T>(T[] s, int i, int j)
        {
            T num = s[i];
            s[i] = s[j];
            s[j] = num;
        }
    }
}
