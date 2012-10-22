using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    class IncompleteMessageException : Exception
    {
        private int code;
        private string message;
        private string input;


        public IncompleteMessageException(string message, int code = 0)
        {
            this.message = message;
            this.code = code;
        }

        public void setInput(string input)
        {
            this.input = input;
        }

        public string getInput()
        {
            return this.input;
        }

    }
}
