using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WhatsAppApi.Account;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;

namespace WhatsAppPort
{
    public partial class frmUserChat : Form
    {
        //public delegate void StringDelegate(string value);
        //public delegate void ProtocolDelegate(ProtocolTreeNode node);

        //public event StringDelegate MessageSentEvent;
        //public event Action MessageAckEvent;
        //public event ProtocolDelegate MessageRecievedEvent;
        private User user;
        private bool isTyping;

        public frmUserChat(User user)
        {
            InitializeComponent();
            this.user = user;
            this.isTyping = false;
            WhatsEventHandler.MessageRecievedEvent += WhatsEventHandlerOnMessageRecievedEvent;
            WhatsEventHandler.IsTypingEvent += WhatsEventHandlerOnIsTypingEvent;
        }

        private void WhatsEventHandlerOnIsTypingEvent(string @from, bool value)
        {
            if (!this.user.WhatsUser.GetFullJid().Equals(from))
                return;

            this.lblIsTyping.Visible = value;
        }
        private void WhatsEventHandlerOnMessageRecievedEvent(FMessage mess)
        {
            var tmpMes = mess.data;
            this.AddNewText(this.user.UserName, tmpMes);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (this.txtBxSentText.Text.Length == 0)
                return;

            WhatSocket.Instance.SendMessage(this.user.WhatsUser.GetFullJid(), txtBxSentText.Text);
            this.AddNewText(this.user.UserName, txtBxSentText.Text);
            txtBxSentText.Clear();
        }

        private void AddNewText(string from, string text)
        {
            this.txtBxChat.AppendText(string.Format("{0}: {1}{2}", from, text, Environment.NewLine));
        }

        private void txtBxSentText_TextChanged(object sender, EventArgs e)
        {
            if (!this.isTyping)
            {
                this.isTyping = true;
                WhatSocket.Instance.SendComposing(this.user.WhatsUser.GetFullJid());
                this.timerTyping.Start();
            }
        }

        private void timerTyping_Tick(object sender, EventArgs e)
        {
            if (this.isTyping)
            {
                this.isTyping = false;
                return;
            }
            WhatSocket.Instance.SendPaused(this.user.WhatsUser.GetFullJid());
            this.timerTyping.Stop();
        }
    }
}
