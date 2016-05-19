using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    public sealed class KeyValue
    {
        public string Key { get; private set; }
        public string Value { get; private set; }

        public KeyValue(string key, string value)
        {
            if ((value == null) || (key == null))
            {
                throw new NullReferenceException();
            }
            this.Key = key;
            this.Value = value;
        }
    }
}
