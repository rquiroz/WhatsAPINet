using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppPort
{
    public class WhatSocket
    {
        private static WhatsAppApi.WhatsApp _instance;

        public static WhatsAppApi.WhatsApp Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }
                else
                {
                    throw new Exception("Instance is not set");
                }
            }
        }

        public static void Create(string username, string password, string nickname, bool debug = false)
        {
            _instance = new WhatsAppApi.WhatsApp(username, password, nickname, debug);
        }
    }
}
