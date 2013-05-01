﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Chat.Client.GUI
{
    public partial class ClientWindow : Form
    {
        public ClientWindow()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Connect();
        }

        private void Connect()
        {
            var connectDialog = new ConnectDialog();

            if (DialogResult.OK == connectDialog.ShowDialog())
            {

            }
        }
    }
}
