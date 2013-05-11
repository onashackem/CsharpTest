using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Chat.Server;
using Chat.Client.Messages;

namespace Chat.Client.GUI
{
    public partial class ClientWindow : Form
    {
        private readonly Client client1 = new Client() { Name = "C1" };
        private readonly Client client2 = new Client() { Name = "C2" };

        private readonly Client client = new Client() { Name = "Client" };
        private string userName = string.Empty;

        private readonly List<Brush> brushes = new List<Brush>() {
            Brushes.DarkViolet,            
            Brushes.DarkSlateGray,
            Brushes.DarkGoldenrod,
            Brushes.DarkOrange,
            Brushes.DarkOliveGreen,
            Brushes.DarkRed,
            Brushes.DarkBlue
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
                    var name = connectDialog.GetName();
                    isNameValid = !String.IsNullOrEmpty(name) && !name.Contains(" ");

                    if (!isNameValid)
                    {
                        MessageBox.Show("You have to choose some nick without spaces.", "Invalid name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }

                    if (!client.TryConnect(connectDialog.GetAddress()))
                    {
                        MessageBox.Show("Couldn't connect.", "Invalis address", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }
                }
            }

            // Store valid user name
            userName = connectDialog.GetName();

            MessageBox.Show("You can chat now, " + userName + " :)", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

            FinishInitialization();
        }

        private void FinishInitialization()
        {
            this.Text += " - " + userName;
            this.tbxName.Text = userName;
            tbxMessage.Focus();

            client.Name = userName;

            // React on data received by client
            client.ChatMessageReceived += new EventHandler<ChatMessageEventArgs>(client_MessageRecived);
            client.ErrorMessageReceived += new EventHandler<ErrorMessageEventArgs>(client_ErrorMessageReceived);
        }

        void client_ErrorMessageReceived(object sender, ErrorMessageEventArgs e)
        {
            var message = String.Format("[{0} | Error message received] {1}\n\n Disconnected. Try relogin.", DateTime.Now, e.Error);
            
            DisplayMessage(message);

            client.Disconnect();
        }

        private void client_MessageRecived(object sender, ChatMessageEventArgs e)
        {
            var message = String.Format("[{0} | {1}] {2}", DateTime.Now, e.User, e.Message);

            DisplayMessage(message);
        }

        private void DisplayMessage(string message)
        {
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
            this.lbxChat.SelectedIndex = this.lbxChat.Items.Count - 1;
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

        private void SendMessage()
        {
            var data = this.tbxMessage.Text;
            tbxMessage.Text = string.Empty;
            tbxMessage.Focus();

            // Nothing to send
            if (String.IsNullOrEmpty(data))
                return;

            if (!client.Connected)
                return;

            // Send chat message
            client.SendMessage(new ChatMessage(userName, data + "\n"));
        }
    }
}
