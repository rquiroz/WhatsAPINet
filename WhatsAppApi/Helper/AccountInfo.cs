using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    public class AccountInfo
    {
        //status'=>$node->getAttribute('status'),'kind'=>$node->getAttribute('kind'),'creation'=>$node->getAttribute('creation'),'expiration'=>$node->getAttribute('expiration')
        public string Status { get; private set; }
        public string Kind { get; private set; }
        public string Creation { get; private set; }
        public string Expiration { get; private set; }

        public AccountInfo(string status, string kind, string creation, string expiration)
        {
            this.Status = status;
            this.Kind = kind;
            this.Creation = creation;
            this.Expiration = expiration;
        }

        public new string ToString()
        {
            return string.Format("Status: {0}, Kind: {1}, Creation: {2}, Expiration: {3}",
                                 this.Status,
                                 this.Kind,
                                 this.Creation,
                                 this.Expiration);
        }
    }
}
