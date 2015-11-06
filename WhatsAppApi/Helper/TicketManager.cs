using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace WhatsAppApi.Helper
{
    class TicketManager
    {
        private static TicketManager _instance;
        private string idBase;
        public static string IdBase
        {
            get
            {
                if (_instance == null)
                    _instance = new TicketManager();
                return _instance.idBase;
            }
        }

        public TicketManager()
        {
            idBase = Func.GetNowUnixTimestamp().ToString();
        }

        public static string GenerateId()
        {
            if (_instance == null)
                _instance = new TicketManager();

            return (IdBase + "-" + TicketCounter.NextTicket());
        }
    }

    public static class TicketCounter
    {
        private static int id = 0;
        private static string loginTime;

        public static int NextTicket()
        {
            return Interlocked.Increment(ref id);
        }

        public static void setLoginTime(string Time)
        {
            loginTime = Time;
        }

        public static string MakeId()
        {
            int num = NextTicket();
            return string.Format("{0}-{1}", loginTime, num);
        }
    }
}
