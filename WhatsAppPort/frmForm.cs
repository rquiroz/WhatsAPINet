using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using WhatsAppApi;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;

namespace WhatsAppPort
{
    public partial class frmForm : Form
    {
        private WhatsMessageHandler messageHandler;
        private BackgroundWorker bgWorker;
        private volatile bool isRunning;
        private Dictionary<string, User> userList;

        private string phoneNum;
        private string phonePass;
        private string phoneNick;

        public frmForm(string num, string pass, string nick)
        {
            this.phoneNum = num;
            this.phonePass = pass;
            this.phoneNick = nick;

            InitializeComponent();
            this.userList = new Dictionary<string, User>();
            this.isRunning = true;
            this.bgWorker = new BackgroundWorker();
            this.bgWorker.DoWork += ProcessMessages;
            this.bgWorker.ProgressChanged += NewMessageArrived;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.WorkerReportsProgress = true;
            this.messageHandler = new WhatsMessageHandler();
        }

        private void btnAddContact_Click(object sender, EventArgs e)
        {
            using (var tmpAddUser = new frmAddUser())
            {
                tmpAddUser.ShowDialog(this);
                if (tmpAddUser.DialogResult != DialogResult.OK)
                    return;
                if(tmpAddUser.Tag == null || !(tmpAddUser.Tag is User))
                    return;

                var tmpUser = tmpAddUser.Tag as User;
                this.userList.Add(tmpUser.PhoneNumber, tmpUser);

                var tmpListUser = new ListViewItem(tmpUser.UserName);
                tmpListUser.Tag = tmpUser;
                this.listViewContacts.Items.Add(tmpListUser);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            WhatSocket.Instance.Connect();
            WhatSocket.Instance.Login();
            this.bgWorker.RunWorkerAsync();
        }

        private void ProcessMessages(object sender, DoWorkEventArgs args)
        {
            if(sender == null)
                return;

            while (this.isRunning)
            {
                if (!WhatSocket.Instance.HasMessages())
                {
                    WhatSocket.Instance.PollMessages();
                    Thread.Sleep(100);
                    continue;
                }

                var tmpMessages = WhatSocket.Instance.GetAllMessages();
                (sender as BackgroundWorker).ReportProgress(1, tmpMessages);
            }
        }

        private void NewMessageArrived(object sender, ProgressChangedEventArgs args)
        {
            if (args.UserState == null || !(args.UserState is ProtocolTreeNode[]))
                return;

            var tmpMessages = args.UserState as ProtocolTreeNode[];
            foreach (var protocolNode in tmpMessages)
            {
                this.PopulateNewMessage(protocolNode);
            }
        }

        private void PopulateNewMessage(ProtocolTreeNode protocolNode)
        {
            this.GetMessageType(protocolNode);
            this.GetMessageBody(protocolNode);
            this.GetMessageSender(protocolNode);
        }

        private void GetMessageSender(ProtocolTreeNode protocolNode)
        {
            
        }

        private void GetMessageBody(ProtocolTreeNode protocolNode)
        {
            
        }

        private void GetMessageType(ProtocolTreeNode protocolNode)
        {
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.isRunning = false;
            this.bgWorker.CancelAsync();
            this.bgWorker = null;
        }

        private void listViewContacts_DoubleClick(object sender, EventArgs e)
        {
            if (sender == null || !(sender is ListView))
                return;

            var tmpListView = sender as ListView;
            if (tmpListView.SelectedItems.Count == 0)
                return;

            var selItem = tmpListView.SelectedItems[0];
            var tmpUser = selItem.Tag as User;

            var tmpDialog = new frmUserChat(tmpUser);
            //tmpDialog.MessageRecievedEvent += new frmUserChat.ProtocolDelegate(tmpDialog_MessageRecievedEvent);
            tmpDialog.Show();
        }
    }
}
