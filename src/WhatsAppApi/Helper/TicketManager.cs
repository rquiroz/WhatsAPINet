using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace WhatsAppApi.Helper
{
    class TicketManager
    {
        public static string IdBase { get; private set; }

        public TicketManager()
        {
            IdBase = DateTime.Now.Ticks.ToString();
        }

        public static string GenerateId()
        {
            return (IdBase + "-" + TicketCounter.NextTicket());
        }
    }

    public static class TicketCounter
    {
        private static int id = -1;

        public static int NextTicket()
        {
            return Interlocked.Increment(ref id);
        }

        public static string MakeId(string prefix)
        {
            int num = NextTicket();
            if (true)//this.IsVerboseId)
            {
                return (prefix + num);
            }
            //return num.ToString("X");
        }
    }
}
