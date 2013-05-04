using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Chat.Client.GUI
{
    public partial class ConnectDialog : Form
    {
        public ConnectDialog()
        {
            InitializeComponent();

            this.tbx_Address.Text = Configuration.Configuration.ServerIpV4Address;
        }

        public string GetAddress()
        {
            return this.tbx_Address.Text;
        }

        public string GetName()
        {
            return this.tbx_Name.Text;
        }
    }
}
