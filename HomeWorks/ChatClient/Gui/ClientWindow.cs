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

        private readonly Client2 client = new Client2() { Name = "Client" };
        private string userName = string.Empty;

        private readonly List<Brush> brushes = new List<Brush>() {
            Brushes.Red,
            Brushes.Green,            
            Brushes.Orange,
            Brushes.Blue,
            Brushes.Yellow
        };

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
            bool isNameValid = false;

            // Ask user for data until data are valid
            while (!(client.Connected && isNameValid))
            {
                if (DialogResult.OK == connectDialog.ShowDialog())
                {
                    if (!client.TryConnect(connectDialog.GetAddress()))
                    {
                        MessageBox.Show("Couldn't connect.", "Invalis address", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }
                }

                var name = connectDialog.GetName();
                isNameValid = !String.IsNullOrEmpty(name) && !name.Contains(" ");

                if (!isNameValid)
                {
                    MessageBox.Show("You have to choose some nick without spaces.", "Invalid name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Store valid user name
            userName = connectDialog.GetName();

            MessageBox.Show("You can chat now, " + userName + " :)", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Finalize();
        }

        private void Finalize()
        {
            this.Text += " - " + userName;
            this.tbxName.Text = userName;

            // React on data received by client
            client.MessageRecived += new EventHandler<MessageEventArgs>(client_MessageRecived);            

            ///* TODO: test
            var c1 = client1.TryConnect("127.0.0.1");
            var c2 = client2.TryConnect("127.0.0.1");

            client1.SendMessage("C1, M1\n");
            client2.SendMessage("C2, M1\n");
            client2.SendMessage("C2, M2\n");
            client1.SendMessage("C1, M2\n");
            client2.SendMessage("C2, M3\n");
            //*/
        }

        private void client_MessageRecived(object sender, MessageEventArgs e)
        {
            var message = e.Data;

            if (InvokeRequired)
            {
                Invoke((Action)(() => AddMessage(message)));
            }
            else
            {
                AddMessage(message);
            }
        }

        private void AddMessage(string message)
        {
            this.lbxChat.Items.Add(message);
            this.lbxChat.Refresh();
        }

        private void btnSend_Click(object sender, System.EventArgs e)
        {
            SendMessage();
        }

        private void tbxMessage_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                SendMessage();
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

            // If the ListBox has focus, draw a focus rectangle around the selected item. 
            e.DrawFocusRectangle();
        }

        private void SendMessage()
        {
            var data = this.tbxMessage.Text;
            tbxMessage.Text = string.Empty;

            // Nothing to send
            if (String.IsNullOrEmpty(data))
                return;

            if (!client.Connected)
                return;

            // TODO: message
            client.SendMessage(data + "\n");
        }
    }
}
