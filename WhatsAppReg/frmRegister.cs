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
                this.number = this.txtPhoneNumber.Text;
                this.cc = this.number.Substring(0, 2);
                this.phone = this.number.Substring(2);
                if (WhatsAppApi.Register.WhatsRegisterV2.RequestCode(this.cc, this.phone, method))
                {
                    this.grpStep1.Enabled = false;
                    this.grpStep2.Enabled = true;
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
                    this.txtOutput.Text = String.Format("Found password:\r\n{0}\r\n\r\nWrite it down and exit the program", this.password);
                    this.grpStep2.Enabled = false;
                    this.grpResult.Enabled = true;
                }
            }
        }
    }
}
