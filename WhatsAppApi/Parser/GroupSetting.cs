using System;
using System.Runtime.InteropServices;

namespace WhatsAppApi.Parser
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GroupSetting
    {
        public string Jid;
        public bool Enabled;
        public DateTime? MuteExpiry;
    }
}