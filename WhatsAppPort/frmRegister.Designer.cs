namespace WhatsAppPort
{
    partial class frmRegister
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
            this.txtPhoneNumber = new System.Windows.Forms.TextBox();
            this.btnCodeRequest = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.grpStep1 = new System.Windows.Forms.GroupBox();
            this.grpStep2 = new System.Windows.Forms.GroupBox();
            this.radSMS = new System.Windows.Forms.RadioButton();
            this.radVoice = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btnRegisterCode = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.grpStep1.SuspendLayout();
            this.grpStep2.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtPhoneNumber
            // 
            this.txtPhoneNumber.Location = new System.Drawing.Point(88, 19);
            this.txtPhoneNumber.Name = "txtPhoneNumber";
            this.txtPhoneNumber.Size = new System.Drawing.Size(165, 20);
            this.txtPhoneNumber.TabIndex = 1;
            // 
            // btnCodeRequest
            // 
            this.btnCodeRequest.Location = new System.Drawing.Point(159, 45);
            this.btnCodeRequest.Name = "btnCodeRequest";
            this.btnCodeRequest.Size = new System.Drawing.Size(94, 23);
            this.btnCodeRequest.TabIndex = 2;
            this.btnCodeRequest.Text = "Request code";
            this.btnCodeRequest.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Phone number";
            // 
            // grpStep1
            // 
            this.grpStep1.Controls.Add(this.radVoice);
            this.grpStep1.Controls.Add(this.radSMS);
            this.grpStep1.Controls.Add(this.label2);
            this.grpStep1.Controls.Add(this.btnCodeRequest);
            this.grpStep1.Controls.Add(this.txtPhoneNumber);
            this.grpStep1.Location = new System.Drawing.Point(13, 13);
            this.grpStep1.Name = "grpStep1";
            this.grpStep1.Size = new System.Drawing.Size(259, 76);
            this.grpStep1.TabIndex = 4;
            this.grpStep1.TabStop = false;
            this.grpStep1.Text = "Step 1: Request code";
            // 
            // grpStep2
            // 
            this.grpStep2.Controls.Add(this.textBox2);
            this.grpStep2.Controls.Add(this.btnRegisterCode);
            this.grpStep2.Controls.Add(this.textBox1);
            this.grpStep2.Controls.Add(this.label1);
            this.grpStep2.Location = new System.Drawing.Point(13, 96);
            this.grpStep2.Name = "grpStep2";
            this.grpStep2.Size = new System.Drawing.Size(259, 130);
            this.grpStep2.TabIndex = 5;
            this.grpStep2.TabStop = false;
            this.grpStep2.Text = "Step 2: Confirm code";
            // 
            // radSMS
            // 
            this.radSMS.AutoSize = true;
            this.radSMS.Checked = true;
            this.radSMS.Location = new System.Drawing.Point(9, 50);
            this.radSMS.Name = "radSMS";
            this.radSMS.Size = new System.Drawing.Size(48, 17);
            this.radSMS.TabIndex = 4;
            this.radSMS.TabStop = true;
            this.radSMS.Text = "SMS";
            this.radSMS.UseVisualStyleBackColor = true;
            // 
            // radVoice
            // 
            this.radVoice.AutoSize = true;
            this.radVoice.Location = new System.Drawing.Point(64, 50);
            this.radVoice.Name = "radVoice";
            this.radVoice.Size = new System.Drawing.Size(52, 17);
            this.radVoice.TabIndex = 5;
            this.radVoice.Text = "Voice";
            this.radVoice.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Code";
            // 
            // textBox1
            // 
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(88, 19);
            this.textBox1.MaxLength = 6;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(65, 20);
            this.textBox1.TabIndex = 1;
            // 
            // btnRegisterCode
            // 
            this.btnRegisterCode.Enabled = false;
            this.btnRegisterCode.Location = new System.Drawing.Point(159, 17);
            this.btnRegisterCode.Name = "btnRegisterCode";
            this.btnRegisterCode.Size = new System.Drawing.Size(94, 23);
            this.btnRegisterCode.TabIndex = 2;
            this.btnRegisterCode.Text = "Confirm code";
            this.btnRegisterCode.UseVisualStyleBackColor = true;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(9, 46);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(244, 78);
            this.textBox2.TabIndex = 3;
            // 
            // frmRegister
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 238);
            this.Controls.Add(this.grpStep2);
            this.Controls.Add(this.grpStep1);
            this.Name = "frmRegister";
            this.Text = "Register";
            this.grpStep1.ResumeLayout(false);
            this.grpStep1.PerformLayout();
            this.grpStep2.ResumeLayout(false);
            this.grpStep2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox txtPhoneNumber;
        private System.Windows.Forms.Button btnCodeRequest;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox grpStep1;
        private System.Windows.Forms.RadioButton radSMS;
        private System.Windows.Forms.GroupBox grpStep2;
        private System.Windows.Forms.RadioButton radVoice;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button btnRegisterCode;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
    }
}