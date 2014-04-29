using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WhatsAppApi.Parser
{
    public class PhoneNumber
    {
        public string Country;
        public string CC;
        public string Number;
        public string FullNumber
        {
            get
            {
                return this.CC + this.Number;
            }
        }
        public string ISO3166;
        public string ISO639;
        protected string _mcc;
        protected string _mnc;

        public string MCC
        {
            get
            {
                return this._mcc.PadLeft(3, '0');
            }
        }

        public string MNC
        {
            get
            {
                return this._mnc.PadLeft(3, '0');
            }
        }

        public PhoneNumber(string number)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WhatsAppApi.Parser.countries.csv"))
            {
                using (var reader = new StreamReader(stream))
                {
                    string csv = reader.ReadToEnd();
                    string[] lines = csv.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        string[] values = line.Trim(new char[] { '\r' }).Split(new char[] { ',' });
                        //try to match
                        if (number.StartsWith(values[1]))
                        {
                            //matched
                            this.Country = values[0].Trim(new char[] { '"' });
                            //hook: Fix CC for North America
                            if (values[1].StartsWith("1"))
                            {
                                values[1] = "1";
                            }
                            this.CC = values[1];
                            this.Number = number.Substring(this.CC.Length);
                            this.ISO3166 = values[4].Trim(new char[] { '"' });
                            this.ISO639 = values[5].Trim(new char[] { '"' });
                            this._mcc = values[2].Trim(new char[] { '"' });
                            this._mnc = values[3].Trim(new char[] { '"' });
                            if (this._mcc.Contains('|'))
                            {
                                //take first one
                                string[] parts = this._mcc.Split(new char[] { '|' });
                                this._mcc = parts[0];
                            }
                            return;
                        }
                    }
                    //could not match!
                    throw new Exception(String.Format("Could not dissect phone number {0}", number));
                }
            }
        }
    }
}
