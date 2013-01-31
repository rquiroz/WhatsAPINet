using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WhatsAppApi;
using WhatsAppApi.Account;
using WhatsAppApi.Register;

namespace WhatsTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var tmpEncoding = Encoding.UTF8;
            System.Console.OutputEncoding = Encoding.Default;
            System.Console.InputEncoding = Encoding.Default;
            string nickname = "WhatsAPI Test";
            //string sender = ""; // Mobile number with country code (but without + or 00)
            //string imei = ""; // MAC Address for iOS IMEI for other platform (Android/etc) 

            //WhatsApp wa = new WhatsApp(sender, imei, nickname, true);

            //string countrycode = sender.Substring(0, 2);
            //string phonenumber = sender.Remove(0, 2);

            //if (!WhatsRegister.ExistsAndDelete(countrycode, phonenumber, imei))
            //{
            //    PrintToConsole("Wrong Password");
            //    return;
            //}

            //wa.Connect();
            //wa.Login();
            //wa.sendNickname("test");

            //ProcessChat(wa, "");
            RegisterAccount();

            Console.ReadKey();
        }


        private static void ProcessChat(WhatsApp wa, string dst)
        {
            var thRecv = new Thread(t =>
                                        {
                                            try
                                            {
                                                while (wa != null)
                                                {
                                                    if (!wa.HasMessages())
                                                    {
                                                        wa.PollMessages();
                                                        Thread.Sleep(100);
                                                        continue;
                                                    }
                                                    var buff = wa.GetAllMessages();
                                                }
                                            }
                                            catch (ThreadAbortException)
                                            {
                                            }
                                        }) {IsBackground = true};
            thRecv.Start();
            WhatsUserManager usrMan = new WhatsUserManager();
            var tmpUser = usrMan.CreateUser(dst, "User");

            while (true)
            {
                string line = Console.ReadLine();
                if (line == null && line.Length == 0)
                    continue;

                string command = line.Trim();
                switch (command)
                {
                    case "/query":
                        //var dst = dst//trim(strstr($line, ' ', FALSE));
                        PrintToConsole("[] Interactive conversation with {0}:", tmpUser);
                        break;
                    case "/accountinfo":
                        PrintToConsole("[] Account Info: {0}", wa.GetAccountInfo().ToString());
                        break;
                    case "/lastseen":
                        PrintToConsole("[] Request last seen {0}", tmpUser);
                        wa.RequestLastSeen(tmpUser.GetFullJid());
                        break;
                    case "/exit":
                        wa = null;
                        thRecv.Abort();
                        return;
                    case "/start":
                        wa.WhatsSendHandler.SendComposing(tmpUser.GetFullJid());
                        break;
                    case "/pause":
                        wa.WhatsSendHandler.SendPaused(tmpUser.GetFullJid());
                        break;
                    case "/register":
                        {
                            RegisterAccount();
                            break;
                        }
                    default:
                        PrintToConsole("[] Send message to {0}: {1}", tmpUser, line);
                        wa.Message(tmpUser.GetFullJid(), line);
                        break;
                }
           } 
        }

        private static void RegisterAccount()
        {
            Console.Write("CountryCode (ex. 49): ");
            string countryCode = Console.ReadLine();
            Console.Write("Phonenumber: ");
            string phoneNumber = Console.ReadLine();

            if (!WhatsRegister.RegisterUser(countryCode, phoneNumber))
                return;
            Console.Write("Enter send Code: ");
            string tmpCode = Console.ReadLine();
            Console.Write("Enter your new Password: ");
            string tmpPassword = Console.ReadLine();

            WhatsRegister.VerifyRegistration(countryCode, phoneNumber, tmpPassword, tmpCode);
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void PrintToConsole(string value, params object[] tmpParams)
        {
            Console.WriteLine(value, tmpParams);
        }
    }
}
