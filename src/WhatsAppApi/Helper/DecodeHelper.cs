using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    internal static class DecodeHelper
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
            if (value.Length%count != 0)
            {
                int tmpRest = value.Length%count;
                returnList.Add(value.Substring(value.Length - 1 - tmpRest, tmpRest));
            }
            return returnList.ToArray();
        }

        public static string[] getDictionary()
        {
            if (dictList != null)
                return dictList;

            dictList = new string[249];
            dictList[0] = null;
            dictList[1] = null;
            dictList[2] = null;
            dictList[3] = null;
            dictList[4] = null;
            dictList[5] = "1";
            dictList[6] = "1.0";
            dictList[7] = "ack";
            dictList[8] = "action";
            dictList[9] = "active";
            dictList[10] = "add";
            dictList[11] = "all";
            dictList[12] = "allow";
            dictList[13] = "apple";
            dictList[14] = "audio";
            dictList[15] = "auth";
            dictList[16] = "author";
            dictList[17] = "available";
            dictList[18] = "bad-request";
            dictList[19] = "basee64";
            dictList[20] = "Bell.caf";
            dictList[21] = "bind";
            dictList[22] = "body";
            dictList[23] = "Boing.caf";
            dictList[24] = "cancel";
            dictList[25] = "category";
            dictList[26] = "challenge";
            dictList[27] = "chat";
            dictList[28] = "clean";
            dictList[29] = "code";
            dictList[30] = "composing";
            dictList[31] = "config";
            dictList[32] = "conflict";
            dictList[33] = "contacts";
            dictList[34] = "create";
            dictList[35] = "creation";
            dictList[36] = "default";
            dictList[37] = "delay";
            dictList[38] = "delete";
            dictList[39] = "delivered";
            dictList[40] = "deny";
            dictList[41] = "DIGEST-MD5";
            dictList[42] = "DIGEST-MD5-1";
            dictList[43] = "dirty";
            dictList[44] = "en";
            dictList[45] = "enable";
            dictList[46] = "encoding";
            dictList[47] = "error";
            dictList[48] = "expiration";
            dictList[49] = "expired";
            dictList[50] = "failure";
            dictList[51] = "false";
            dictList[52] = "favorites";
            dictList[53] = "feature";
            dictList[54] = "field";
            dictList[55] = "free";
            dictList[56] = "from";
            dictList[57] = "g.us";
            dictList[58] = "get";
            dictList[59] = "Glas.caf";
            dictList[60] = "google";
            dictList[61] = "group";
            dictList[62] = "groups";
            dictList[63] = "g_sound";
            dictList[64] = "Harp.caf";
            dictList[65] = "http://etherx.jabber.org/streams";
            dictList[66] = "http://jabber.org/protocol/chatstates";
            dictList[67] = "id";
            dictList[68] = "image";
            dictList[69] = "img";
            dictList[70] = "inactive";
            dictList[71] = "internal-server-error";
            dictList[72] = "iq";
            dictList[73] = "item";
            dictList[74] = "item-not-found";
            dictList[75] = "jabber:client";
            dictList[76] = "jabber:iq:last";
            dictList[77] = "jabber:iq:privacy";
            dictList[78] = "jabber:x:delay";
            dictList[79] = "jabber:x:event";
            dictList[80] = "jid";
            dictList[81] = "jid-malformed";
            dictList[82] = "kind";
            dictList[83] = "leave";
            dictList[84] = "leave-all";
            dictList[85] = "list";
            dictList[86] = "location";
            dictList[87] = "max_groups";
            dictList[88] = "max_participants";
            dictList[89] = "max_subject";
            dictList[90] = "mechanism";
            dictList[91] = "mechanisms";
            dictList[92] = "media";
            dictList[93] = "message";
            dictList[94] = "message_acks";
            dictList[95] = "missing";
            dictList[96] = "modify";
            dictList[97] = "name";
            dictList[98] = "not-acceptable";
            dictList[99] = "not-allowed";
            dictList[100] = "not-authorized";
            dictList[101] = "notify";
            dictList[102] = "Offline Storage";
            dictList[103] = "order";
            dictList[104] = "owner";
            dictList[105] = "owning";
            dictList[106] = "paid";
            dictList[107] = "participant";
            dictList[108] = "participants";
            dictList[109] = "participating";
            dictList[110] = "fail";
            dictList[111] = "paused";
            dictList[112] = "picture";
            dictList[113] = "ping";
            dictList[114] = "PLAIN";
            dictList[115] = "platform";
            dictList[116] = "presence";
            dictList[117] = "preview";
            dictList[118] = "probe";
            dictList[119] = "prop";
            dictList[120] = "props";
            dictList[121] = "p_o";
            dictList[122] = "p_t";
            dictList[123] = "query";
            dictList[124] = "raw";
            dictList[125] = "receipt";
            dictList[126] = "receipt_acks";
            dictList[127] = "received";
            dictList[128] = "relay";
            dictList[129] = "remove";
            dictList[130] = "Replaced by new connection";
            dictList[131] = "request";
            dictList[132] = "resource";
            dictList[133] = "resource-constraint";
            dictList[134] = "response";
            dictList[135] = "result";
            dictList[136] = "retry";
            dictList[137] = "rim";
            dictList[138] = "s.whatsapp.net";
            dictList[139] = "seconds";
            dictList[140] = "server";
            dictList[141] = "session";
            dictList[142] = "set";
            dictList[143] = "show";
            dictList[144] = "sid";
            dictList[145] = "sound";
            dictList[146] = "stamp";
            dictList[147] = "starttls";
            dictList[148] = "status";
            dictList[149] = "stream:error";
            dictList[150] = "stream:features";
            dictList[151] = "subject";
            dictList[152] = "subscribe";
            dictList[153] = "success";
            dictList[154] = "system-shutdown";
            dictList[155] = "s_o";
            dictList[156] = "s_t";
            dictList[157] = "t";
            dictList[158] = "TimePassing.caf";
            dictList[159] = "timestamp";
            dictList[160] = "to";
            dictList[161] = "Tri-tone.caf";
            dictList[162] = "type";
            dictList[163] = "unavailable";
            dictList[164] = "uri";
            dictList[165] = "url";
            dictList[166] = "urn:ietf:params:xml:ns:xmpp-bind";
            dictList[167] = "urn:ietf:params:xml:ns:xmpp-sasl";
            dictList[168] = "urn:ietf:params:xml:ns:xmpp-session";
            dictList[169] = "urn:ietf:params:xml:ns:xmpp-stanzas";
            dictList[170] = "urn:ietf:params:xml:ns:xmpp-streams";
            dictList[171] = "urn:xmpp:delay";
            dictList[172] = "urn:xmpp:ping";
            dictList[173] = "urn:xmpp:receipts";
            dictList[174] = "urn:xmpp:whatsapp";
            dictList[175] = "urn:xmpp:whatsapp:dirty";
            dictList[176] = "urn:xmpp:whatsapp:mms";
            dictList[177] = "urn:xmpp:whatsapp:push";
            dictList[178] = "value";
            dictList[179] = "vcard";
            dictList[180] = "version";
            dictList[181] = "video";
            dictList[182] = "w";
            dictList[183] = "w:g";
            dictList[184] = "w:p:r";
            dictList[185] = "wait";
            dictList[186] = "x";
            dictList[187] = "xml-not-well-formed";
            dictList[188] = "xml:lang";
            dictList[189] = "xmlns";
            dictList[190] = "xmlns:stream";
            dictList[191] = "Xylophone.caf";
            dictList[192] = "account";
            dictList[193] = "digest";
            dictList[194] = "g_notify";
            dictList[195] = "method";
            dictList[196] = "password";
            dictList[197] = "registration";
            dictList[198] = "stat";
            dictList[199] = "text";
            dictList[200] = "user";
            dictList[201] = "username";
            dictList[202] = "event";
            dictList[203] = "latitude";
            dictList[204] = "longitude";
            dictList[205] = "true";
            dictList[206] = "after";
            dictList[207] = "before";
            dictList[208] = "broadcast";
            dictList[209] = "count";
            dictList[210] = "features";
            dictList[211] = "first";
            dictList[212] = "index";
            dictList[213] = "invalid-mechanism";
            dictList[214] = "ldictListt";
            dictList[215] = "max";
            dictList[216] = "offline";
            dictList[217] = "proceed";
            dictList[218] = "required";
            dictList[219] = "sync";
            dictList[220] = "elapsed";
            dictList[221] = "ip";
            dictList[222] = "microsoft";
            dictList[223] = "mute";
            dictList[224] = "nokia";
            dictList[225] = "off";
            dictList[226] = "pin";
            dictList[227] = "pop_mean_time";
            dictList[228] = "pop_plus_minus";
            dictList[229] = "port";
            dictList[230] = "reason";
            dictList[231] = "server-error";
            dictList[232] = "silent";
            dictList[233] = "timeout";
            dictList[234] = "lc";
            dictList[235] = "lg";
            dictList[236] = "bad-protocol";
            dictList[237] = "none";
            dictList[238] = "remote-server-timeout";
            dictList[239] = "service-unavailable";
            dictList[240] = "w:p";
            dictList[241] = "w:profile:picture";
            dictList[242] = "notification";
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
