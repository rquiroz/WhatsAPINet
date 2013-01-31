using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Response
{
    class CorruptStreamException : Exception
    {
        public string Message { get; private set; }

        public CorruptStreamException(string pMessage)
        {
            // TODO: Complete member initialization
            this.Message = pMessage;
        }
    }
}
