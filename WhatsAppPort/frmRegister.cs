using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WhatsAppPort
{
    public partial class frmRegister : Form
    {
        public string number;
        public string password;

        public frmRegister(string number)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(number))
            {
                this.number = number;
                this.txtPhoneNumber.Text = number;
            }
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
                string cc = this.number.Substring(0, 2);
                string phone = this.number.Substring(2);
                if (WhatsAppApi.Register.WhatsRegisterV2.RequestCode(cc, phone, method))
                {
                    this.grpStep1.Enabled = false;
                    this.grpStep2.Enabled = true;
                }
            }
        }

        private void btnRegisterCode_Click(object sender, EventArgs e)
        {
            this.grpStep2.Enabled = false;
            this.grpResult.Enabled = true;
        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            this.password = "Derp";
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
