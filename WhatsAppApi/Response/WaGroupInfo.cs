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

        internal WaGroupInfo(string id)
        {
            this.id = id;
        }

        internal WaGroupInfo(string id, string owner, string creation, string subject, string subjectChanged, string subjectChangedBy)
        {
            this.id = id;
            this.owner = owner;
            long.TryParse(creation, out this.creation);
            this.subject = subject;
            long.TryParse(subjectChanged, out this.subjectChangedTime);
            this.subjectChangedBy = subjectChangedBy;
        }
    }
}
