using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    public static class DecodeHelper
    {
        private static string[] dictList = null;
        public static string decode(string hex)
        {
            string[] tmpSub = hex.SplitStringN(2);
            var strBuilder = new StringBuilder();
            foreach (var s in tmpSub)
            {
                strBuilder.AppendFormat("  {0}", getToken(Int32.Parse(s, NumberStyles.HexNumber)));
            }
            return strBuilder.ToString();
        }

        public static string[] SplitStringN(this string value, int count)
        {
            var returnList = new List<string>();
            for (int i = 0; i < value.Length; i += count)
            {
                returnList.Add(value.Substring(i, count));
            }
            if (value.Length % count != 0)
            {
                int tmpRest = value.Length % count;
                returnList.Add(value.Substring(value.Length - 1 - tmpRest, tmpRest));
            }
            return returnList.ToArray();
        }

        public static string[] getDictionary()
        {
            if (dictList != null)
            {
                return dictList;
            }
            dictList = new string[249];
            dictList[0] = null;
            dictList[1] = null;
            dictList[2] = null;
            dictList[3] = null;
            dictList[4] = null;
            dictList[5] = "account";
            dictList[6] = "ack";
            dictList[7] = "action";
            dictList[8] = "active";
            dictList[9] = "add";
            dictList[10] = "after";
            dictList[11] = "ib";
            dictList[12] = "all";
            dictList[13] = "allow";
            dictList[14] = "apple";
            dictList[15] = "audio";
            dictList[16] = "auth";
            dictList[17] = "author";
            dictList[18] = "available";
            dictList[19] = "bad-protocol";
            dictList[20] = "bad-request";
            dictList[21] = "before";
            dictList[22] = "Bell.caf";
            dictList[23] = "body";
            dictList[24] = "Boing.caf";
            dictList[25] = "cancel";
            dictList[26] = "category";
            dictList[27] = "challenge";
            dictList[28] = "chat";
            dictList[29] = "clean";
            dictList[30] = "code";
            dictList[31] = "composing";
            dictList[32] = "config";
            dictList[33] = "conflict";
            dictList[34] = "contacts";
            dictList[35] = "count";
            dictList[36] = "create";
            dictList[37] = "creation";
            dictList[38] = "default";
            dictList[39] = "delay";
            dictList[40] = "delete";
            dictList[41] = "delivered";
            dictList[42] = "deny";
            dictList[43] = "digest";
            dictList[44] = "DIGEST-MD5-1";
            dictList[45] = "DIGEST-MD5-2";
            dictList[46] = "dirty";
            dictList[47] = "elapsed";
            dictList[48] = "broadcast";
            dictList[49] = "enable";
            dictList[50] = "encoding";
            dictList[51] = "duplicate";
            dictList[52] = "error";
            dictList[53] = "event";
            dictList[54] = "expiration";
            dictList[55] = "expired";
            dictList[56] = "fail";
            dictList[57] = "failure";
            dictList[58] = "false";
            dictList[59] = "favorites";
            dictList[60] = "feature";
            dictList[61] = "features";
            dictList[62] = "field";
            dictList[63] = "first";
            dictList[64] = "free";
            dictList[65] = "from";
            dictList[66] = "g.us";
            dictList[67] = "get";
            dictList[68] = "Glass.caf";
            dictList[69] = "google";
            dictList[70] = "group";
            dictList[71] = "groups";
            dictList[72] = "g_notify";
            dictList[73] = "g_sound";
            dictList[74] = "Harp.caf";
            dictList[75] = "http://etherx.jabber.org/streams";
            dictList[76] = "http://jabber.org/protocol/chatstates";
            dictList[77] = "id";
            dictList[78] = "image";
            dictList[79] = "img";
            dictList[80] = "inactive";
            dictList[81] = "index";
            dictList[82] = "internal-server-error";
            dictList[83] = "invalid-mechanism";
            dictList[84] = "ip";
            dictList[85] = "iq";
            dictList[86] = "item";
            dictList[87] = "item-not-found";
            dictList[88] = "user-not-found";
            dictList[89] = "jabber:iq:last";
            dictList[90] = "jabber:iq:privacy";
            dictList[91] = "jabber:x:delay";
            dictList[92] = "jabber:x:event";
            dictList[93] = "jid";
            dictList[94] = "jid-malformed";
            dictList[95] = "kind";
            dictList[96] = "last";
            dictList[97] = "latitude";
            dictList[98] = "lc";
            dictList[99] = "leave";
            dictList[100] = "leave-all";
            dictList[101] = "lg";
            dictList[102] = "list";
            dictList[103] = "location";
            dictList[104] = "longitude";
            dictList[105] = "max";
            dictList[106] = "max_groups";
            dictList[107] = "max_participants";
            dictList[108] = "max_subject";
            dictList[109] = "mechanism";
            dictList[110] = "media";
            dictList[111] = "message";
            dictList[112] = "message_acks";
            dictList[113] = "method";
            dictList[114] = "microsoft";
            dictList[115] = "missing";
            dictList[116] = "modify";
            dictList[117] = "mute";
            dictList[118] = "name";
            dictList[119] = "nokia";
            dictList[120] = "none";
            dictList[121] = "not-acceptable";
            dictList[122] = "not-allowed";
            dictList[123] = "not-authorized";
            dictList[124] = "notification";
            dictList[125] = "notify";
            dictList[126] = "off";
            dictList[127] = "offline";
            dictList[128] = "order";
            dictList[129] = "owner";
            dictList[130] = "owning";
            dictList[131] = "paid";
            dictList[132] = "participant";
            dictList[133] = "participants";
            dictList[134] = "participating";
            dictList[135] = "password";
            dictList[136] = "paused";
            dictList[137] = "picture";
            dictList[138] = "pin";
            dictList[139] = "ping";
            dictList[140] = "platform";
            dictList[141] = "pop_mean_time";
            dictList[142] = "pop_plus_minus";
            dictList[143] = "port";
            dictList[144] = "presence";
            dictList[145] = "preview";
            dictList[146] = "probe";
            dictList[147] = "proceed";
            dictList[148] = "prop";
            dictList[149] = "props";
            dictList[150] = "p_o";
            dictList[151] = "p_t";
            dictList[152] = "query";
            dictList[153] = "raw";
            dictList[154] = "reason";
            dictList[155] = "receipt";
            dictList[156] = "receipt_acks";
            dictList[157] = "received";
            dictList[158] = "registration";
            dictList[159] = "relay";
            dictList[160] = "remote-server-timeout";
            dictList[161] = "remove";
            dictList[162] = "Replaced by new connection";
            dictList[163] = "request";
            dictList[164] = "required";
            dictList[165] = "resource";
            dictList[166] = "resource-constraint";
            dictList[167] = "response";
            dictList[168] = "result";
            dictList[169] = "retry";
            dictList[170] = "rim";
            dictList[171] = "s.whatsapp.net";
            dictList[172] = "s.us";
            dictList[173] = "seconds";
            dictList[174] = "server";
            dictList[175] = "server-error";
            dictList[176] = "service-unavailable";
            dictList[177] = "set";
            dictList[178] = "show";
            dictList[179] = "sid";
            dictList[180] = "silent";
            dictList[181] = "sound";
            dictList[182] = "stamp";
            dictList[183] = "unsubscribe";
            dictList[184] = "stat";
            dictList[185] = "status";
            dictList[186] = "stream:error";
            dictList[187] = "stream:features";
            dictList[188] = "subject";
            dictList[189] = "subscribe";
            dictList[190] = "success";
            dictList[191] = "sync";
            dictList[192] = "system-shutdown";
            dictList[193] = "s_o";
            dictList[194] = "s_t";
            dictList[195] = "t";
            dictList[196] = "text";
            dictList[197] = "timeout";
            dictList[198] = "TimePassing.caf";
            dictList[199] = "timestamp";
            dictList[200] = "to";
            dictList[201] = "Tri-tone.caf";
            dictList[202] = "true";
            dictList[203] = "type";
            dictList[204] = "unavailable";
            dictList[205] = "uri";
            dictList[206] = "url";
            dictList[207] = "urn:ietf:params:xml:ns:xmpp-sasl";
            dictList[208] = "urn:ietf:params:xml:ns:xmpp-stanzas";
            dictList[209] = "urn:ietf:params:xml:ns:xmpp-streams";
            dictList[210] = "urn:xmpp:delay";
            dictList[211] = "urn:xmpp:ping";
            dictList[212] = "urn:xmpp:receipts";
            dictList[213] = "urn:xmpp:whatsapp";
            dictList[214] = "urn:xmpp:whatsapp:account";
            dictList[215] = "urn:xmpp:whatsapp:dirty";
            dictList[216] = "urn:xmpp:whatsapp:mms";
            dictList[217] = "urn:xmpp:whatsapp:push";
            dictList[218] = "user";
            dictList[219] = "username";
            dictList[220] = "value";
            dictList[221] = "vcard";
            dictList[222] = "version";
            dictList[223] = "video";
            dictList[224] = "w";
            dictList[225] = "w:g";
            dictList[226] = "w:p";
            dictList[227] = "w:p:r";
            dictList[228] = "w:profile:picture";
            dictList[229] = "wait";
            dictList[230] = "x";
            dictList[231] = "xml-not-well-formed";
            dictList[232] = "xmlns";
            dictList[233] = "xmlns:stream";
            dictList[234] = "Xylophone.caf";
            dictList[235] = "1";
            dictList[236] = "WAUTH-1";
            dictList[237] = null;
            dictList[238] = null;
            dictList[239] = null;
            dictList[240] = null;
            dictList[241] = null;
            dictList[242] = null;
            dictList[243] = null;
            dictList[244] = null;
            dictList[245] = null;
            dictList[246] = null;
            dictList[247] = null;
            dictList[248] = "XXX";
            return dictList;
        }

        public static string getToken(int index)
        {
            string[] dicList = getDictionary();
            return dicList[index];
        }
    }
}
