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
    public partial class frmLogin : Form
    {
        public frmLogin()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (!this.CheckLogin(this.textBoxPhone.Text, this.textBoxPass.Text))
            {
                MessageBox.Show(this, "Login failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var frm = new frmForm(this.textBoxPhone.Text, this.textBoxPass.Text, this.textBoxNick.Text))
            {
                this.Visible = false;
                frm.ShowDialog();

                this.Visible = true;
                this.BringToFront();
            }
        }

        private bool CheckLogin(string user, string pass)
        {
            try
            {
                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                    return false;

                WhatSocket.Create(user, pass, this.textBoxNick.Text, true);
                WhatSocket.Instance.Connect();
                WhatSocket.Instance.Login();
                //check login status
                if (WhatSocket.Instance.ConnectionStatus == WhatsAppApi.WhatsApp.CONNECTION_STATUS.LOGGEDIN)
                {
                    return true;
                }
            }
            catch (Exception)
            { }
            return false;
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            frmRegister regForm = new frmRegister(this.textBoxPhone.Text);
            DialogResult regResult = regForm.ShowDialog(this);
            if (regResult == System.Windows.Forms.DialogResult.OK)
            {
                this.textBoxPass.Text = regForm.password;
            }
        }
    }
}
