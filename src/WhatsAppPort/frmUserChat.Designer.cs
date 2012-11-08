namespace WhatsAppPort
{
    partial class frmUserChat
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.txtBxChat = new System.Windows.Forms.TextBox();
            this.txtBxSentText = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.lblIsTyping = new System.Windows.Forms.Label();
            this.timerTyping = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // txtBxChat
            // 
            this.txtBxChat.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBxChat.BackColor = System.Drawing.SystemColors.Window;
            this.txtBxChat.Location = new System.Drawing.Point(12, 12);
            this.txtBxChat.Multiline = true;
            this.txtBxChat.Name = "txtBxChat";
            this.txtBxChat.ReadOnly = true;
            this.txtBxChat.Size = new System.Drawing.Size(516, 227);
            this.txtBxChat.TabIndex = 0;
            this.txtBxChat.TabStop = false;
            // 
            // txtBxSentText
            // 
            this.txtBxSentText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBxSentText.Location = new System.Drawing.Point(12, 245);
            this.txtBxSentText.Multiline = true;
            this.txtBxSentText.Name = "txtBxSentText";
            this.txtBxSentText.Size = new System.Drawing.Size(516, 69);
            this.txtBxSentText.TabIndex = 0;
            this.txtBxSentText.TextChanged += new System.EventHandler(this.txtBxSentText_TextChanged);
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Location = new System.Drawing.Point(453, 320);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "Senden";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // lblIsTyping
            // 
            this.lblIsTyping.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblIsTyping.AutoSize = true;
            this.lblIsTyping.Location = new System.Drawing.Point(12, 325);
            this.lblIsTyping.Name = "lblIsTyping";
            this.lblIsTyping.Size = new System.Drawing.Size(91, 13);
            this.lblIsTyping.TabIndex = 0;
            this.lblIsTyping.Text = "Schreibt gerade...";
            this.lblIsTyping.Visible = false;
            // 
            // timerTyping
            // 
            this.timerTyping.Interval = 2000;
            this.timerTyping.Tick += new System.EventHandler(this.timerTyping_Tick);
            // 
            // frmUserChat
            // 
            this.AcceptButton = this.btnSend;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 355);
            this.Controls.Add(this.lblIsTyping);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtBxSentText);
            this.Controls.Add(this.txtBxChat);
            this.Name = "frmUserChat";
            this.Text = "Benutzerchat";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtBxChat;
        private System.Windows.Forms.TextBox txtBxSentText;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Label lblIsTyping;
        private System.Windows.Forms.Timer timerTyping;
    }
}