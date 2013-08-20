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
        public string password;

        public frmRegister(string number)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(number))
            {
                this.txtPhoneNumber.Text = number;
            }
        }
    }
}
