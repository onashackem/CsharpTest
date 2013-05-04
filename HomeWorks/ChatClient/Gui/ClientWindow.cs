using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Chat.Server;

namespace Chat.Client.GUI
{
    public partial class ClientWindow : Form
    {
        private readonly Client2 client1 = new Client2() { Name = "C1" };
        private readonly Client2 client2 = new Client2() { Name = "C2" };

        public ClientWindow()
        {
            InitializeComponent();

            client1.Run();
            client2.Run();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            client1.SendMessage("C1, M1\n");
            client2.SendMessage("C2, M1\n");
            client2.SendMessage("C2, M2\n");
            client1.SendMessage("C1, M2\n");
            client2.SendMessage("C2, M3\n");

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
