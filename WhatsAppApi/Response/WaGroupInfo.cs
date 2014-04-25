using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Response
{
    public class WaGroupInfo
    {
        public readonly string id;
        public readonly string owner;
        public readonly long creation;
        public readonly string subject;
        public readonly long subjectChangedTime;
        public readonly string subjectChangedBy;

        internal WaGroupInfo(string id, string owner, long creation, string subject, long subjectChanged, string subjectChangedBy)
        {
            this.id = id;
            this.owner = owner;
            this.creation = creation;
            this.subject = subject;
            this.subjectChangedTime = subjectChanged;
            this.subjectChangedBy = subjectChangedBy;
        }
    }
}
