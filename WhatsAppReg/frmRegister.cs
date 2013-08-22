using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WhatsAppReg
{
    public partial class frmRegister : Form
    {
        protected string number;
        protected string cc;
        protected string phone;
        protected string password;
        protected string language;
        protected string locale;
        protected string mcc;

        public frmRegister()
        {
            InitializeComponent();
        }

        private void btnCodeRequest_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(this.txtPhoneNumber.Text))
            {
                string method = "sms";
                if (this.radVoice.Checked)
                {
                    method = "voice";
                }
                try
                {
                    WhatsAppApi.Parser.PhoneNumber phonenumber = new WhatsAppApi.Parser.PhoneNumber(this.txtPhoneNumber.Text);
                    this.number = phonenumber.FullNumber;
                    this.cc = phonenumber.CC;
                    this.phone = phonenumber.Number;
                    this.language = phonenumber.ISO639;
                    this.locale = phonenumber.ISO3166;
                    this.mcc = phonenumber.MCC;
                }
                catch (Exception ex)
                {
                    this.txtOutput.Text = String.Format("Error: {0}", ex.Message);
                    return;
                }
                if (WhatsAppApi.Register.WhatsRegisterV2.RequestCode(this.cc, this.phone, out this.password, method, null, this.language, this.locale, this.mcc))
                {
                    if (!string.IsNullOrEmpty(this.password))
                    {
                        //password received
                        this.OnReceivePassword();
                    }
                    else
                    {
                        this.grpStep1.Enabled = false;
                        this.grpStep2.Enabled = true;
                    }
                }
            }
        }

        private void btnRegisterCode_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(this.txtCode.Text) && this.txtCode.Text.Length == 6)
            {
                string code = this.txtCode.Text;
                this.password = WhatsAppApi.Register.WhatsRegisterV2.RegisterCode(this.cc, this.phone, code);
                if (!String.IsNullOrEmpty(this.password))
                {
                    this.OnReceivePassword();
                }
            }
        }

        private void OnReceivePassword()
        {
            this.txtOutput.Text = String.Format("Found password:\r\n{0}\r\n\r\nWrite it down and exit the program", this.password);
            this.grpStep1.Enabled = false;
            this.grpStep2.Enabled = false;
            this.grpResult.Enabled = true;
        }
    }
}
