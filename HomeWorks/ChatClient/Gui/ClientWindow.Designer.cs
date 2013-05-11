using System.Windows.Forms;
using System.Drawing;
namespace Chat.Client.GUI
{
    partial class ClientWindow
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
            this.tbxName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbxMessage = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.lbxChat = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // tbxName
            // 
            this.tbxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxName.Enabled = false;
            this.tbxName.Location = new System.Drawing.Point(80, 10);
            this.tbxName.Name = "tbxName";
            this.tbxName.Size = new System.Drawing.Size(459, 20);
            this.tbxName.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Your name:";
            // 
            // tbxMessage
            // 
            this.tbxMessage.AcceptsTab = true;
            this.tbxMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxMessage.Location = new System.Drawing.Point(13, 471);
            this.tbxMessage.Name = "tbxMessage";
            this.tbxMessage.Size = new System.Drawing.Size(445, 20);
            this.tbxMessage.TabIndex = 0;
            this.tbxMessage.KeyUp += new System.Windows.Forms.KeyEventHandler(this.tbxMessage_KeyUp);
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Location = new System.Drawing.Point(464, 468);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // lbxChat
            // 
            this.lbxChat.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbxChat.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.lbxChat.FormattingEnabled = true;
            this.lbxChat.HorizontalScrollbar = true;
            this.lbxChat.Location = new System.Drawing.Point(16, 36);
            this.lbxChat.Name = "lbxChat";
            this.lbxChat.Size = new System.Drawing.Size(523, 420);
            this.lbxChat.TabIndex = 2;
            this.lbxChat.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbxChat_DrawItem);
            this.lbxChat.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.lbxChat_MeasureItem);
            // 
            // ClientWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(551, 503);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.tbxMessage);
            this.Controls.Add(this.lbxChat);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbxName);
            this.Name = "ClientWindow";
            this.Text = "Nprg083 Chat";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        void lbxChat_MeasureItem(object sender, System.Windows.Forms.MeasureItemEventArgs e)
        {
             var g = e.Graphics; 

            // Count dimensions for string
            var size = g.MeasureString(
                this.lbxChat.Items[e.Index] as string,
                this.lbxChat.Font,
                this.lbxChat.Width - 5 - SystemInformation.VerticalScrollBarWidth);
            
            e.ItemHeight = (int)size.Height + 5;
            e.ItemWidth = (int)size.Width + 5;
        }

        private void lbxChat_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            e.DrawBackground();

            // Color based on item index
            Brush myBrush = brushes[e.Index % brushes.Count];

            // Draw the current item text based on the current Font and the custom brush settings. 
            e.Graphics.DrawString(
                this.lbxChat.Items[e.Index] as string,
                this.lbxChat.Font,
                myBrush,
                new RectangleF(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height)
            );
        }

        #endregion

        private System.Windows.Forms.TextBox tbxName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbxMessage;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.ListBox lbxChat;
    }
}

