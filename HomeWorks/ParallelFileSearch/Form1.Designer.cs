﻿namespace ParallelFileSearch
{
    partial class ParallelFileSearchForm
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
            this.lbx_Files = new System.Windows.Forms.ListBox();
            this.lbl_Stats = new System.Windows.Forms.Label();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // lbx_Files
            // 
            this.lbx_Files.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbx_Files.FormattingEnabled = true;
            this.lbx_Files.Location = new System.Drawing.Point(16, 12);
            this.lbx_Files.Name = "lbx_Files";
            this.lbx_Files.Size = new System.Drawing.Size(256, 212);
            this.lbx_Files.TabIndex = 0;
            this.lbx_Files.Sorted = true;
            this.lbx_Files.DisplayMember = "Name";
            // 
            // label1
            // 
            this.lbl_Stats.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbl_Stats.AutoSize = true;
            this.lbl_Stats.Location = new System.Drawing.Point(13, 237);
            this.lbl_Stats.Name = "lbl_Stats";
            this.lbl_Stats.Size = new System.Drawing.Size(47, 13);
            this.lbl_Stats.TabIndex = 1;
            // 
            // timer
            // 
            this.timer.Interval = 1000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // ParallelFileSearchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.lbl_Stats);
            this.Controls.Add(this.lbx_Files);
            this.Name = "ParallelFileSearchForm";
            this.Text = "ParallelFileSearch";
            this.Load += new System.EventHandler(this.ParallelFileSearchForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lbx_Files;
        private System.Windows.Forms.Label lbl_Stats;
        private System.Windows.Forms.Timer timer;
    }
}

