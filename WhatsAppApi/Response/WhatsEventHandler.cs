using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using WhatsAppApi.Parser;

namespace WhatsAppApi.Response
{
    public static class WhatsEventHandler
    {
        #region Delegates
        public delegate void MessageRecievedHandler(FMessage mess);
        public delegate void StringArrayHandler(string[] value);
        public delegate void BoolHandler(string from, bool value);
        public delegate void PhotoChangedHandler(string from, string uJid, string photoId);
        public delegate void GroupNewSubjectHandler(string from, string uJid, string subject, int t);

        #endregion

        #region Events
        public static event MessageRecievedHandler MessageRecievedEvent;
        public static event BoolHandler IsTypingEvent;
        public static event GroupNewSubjectHandler GroupNewSubjectEvent;
        public static event PhotoChangedHandler PhotoChangedEvent;

        #endregion

        #region OnMethods
        internal static void OnMessageRecievedEventHandler(FMessage mess)
        {
            var h = MessageRecievedEvent;
            if (h == null)
                return;
            foreach (var tmpSingleCast in h.GetInvocationList())
            {
                var tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.BeginInvoke(tmpSingleCast, new object[] {mess});
                    continue;
                }
                h.BeginInvoke(mess, null, null);
            }
        }

        internal static void OnPhotoChangedEventHandler(FMessage mess)
        {
            var h = MessageRecievedEvent;
            if (h == null)
                return;
            foreach (var tmpSingleCast in h.GetInvocationList())
            {
                var tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.BeginInvoke(tmpSingleCast, new object[] { mess });
                    continue;
                }
                h.BeginInvoke(mess, null, null);
            }
        }

        internal static void OnIsTypingEventHandler(string from, bool isTyping)
        {
            var h = IsTypingEvent;
            if (h == null)
                return;
            foreach (var tmpSingleCast in h.GetInvocationList())
            {
                var tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.BeginInvoke(tmpSingleCast, new object[] { from, isTyping });
                    continue;
                }
                h.BeginInvoke(from, isTyping, null, null);
            }
        }

        internal static void OnGroupNewSubjectEventHandler(string from, string uJid, string subject, int t)
        {
            var h = GroupNewSubjectEvent;
            if (h == null)
                return;
            foreach (var tmpSingleCast in h.GetInvocationList())
            {
                var tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.BeginInvoke(tmpSingleCast, new object[] { from, uJid, subject, t });
                    continue;
                }
                h.BeginInvoke(from, uJid, subject, t, null, null);
            }
        }

        internal static void OnPhotoChangedEventHandler(string from, string uJid, string photoId)
        {
            var h = PhotoChangedEvent;
            if (h == null)
                return;
            foreach (var tmpSingleCast in h.GetInvocationList())
            {
                var tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.BeginInvoke(tmpSingleCast, new object[] { from, uJid, photoId });
                    continue;
                }
                h.BeginInvoke(from, uJid, photoId, null, null);
            }
        }


        #endregion
    }
}
