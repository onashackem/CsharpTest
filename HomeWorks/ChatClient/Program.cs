using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Chat.Client;
using Chat.Client.GUI;

namespace Chat
{
    /*
     * Klient ma pole/okno pro zadani adresy serveru 1 tlacitko connect (asynchronni prihlaseni), pak zmizi po pripojeni
     * Klient ma pole pro jmeno, listbox pro zpravy a pole pro zadani zpravy + tlacitko Send
     * 
     * Klient posle zpravu rovnou na server, ktery ji rozesle vsem posluchacum.
     * Zprava se zobrazi az po obdrzeni ze serveru
     * 
     * Od verze 1.1 jde o PINGani na server
     * 
     * Klient a server jsou jeden exe soubor
     * Dotazat se, zda bezet v rezimu server + client / jen client
     * 
     *
     * TCPIP, na portu 4586, pro IPv4/6, zprávy v UTF-8, LE
     * Protokoly 1.0 nebo  1.1
     * 
     * kazda message na 1 radek
     * 
     * Navazani spojeni
     *  - klient: HELLO Nprg038Chat Verze1 Verze2 Verze3
     *      - server: ERROR message (pokud nerozumi) + uzavrit spojeni
     *      - OLLEH Nprg038Chat VerzeServeru (vybere z verzi klienta)
     *          klient ACK
     *  
     *  - MSG jmeno message
     *  
     * od verze 1.1:
     *  - klient kazdou minutu posila PING (pokud neposle message mezitim)
     *  - server odpovi PONG
     *  - pokud server od klienta neuslysi 3 Minuty, odpoji se od nej
     *  
     * omezeni
     *  - asynchronni server (NE co klient to vlakno)
     *  
     * tridy:
     *  - TcpClient (.NetworkStream)
     *  - TcpListener (.NetworkStream)
     *      - IPAddress.Any (.loopback)
     */
    static class Program
    {
        private static Chat.Server.Server server = new Chat.Server.Server();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (DialogResult.Yes ==
                MessageBox.Show(
                    "If you click on Yes, you will run both client and server. If you click NO, you will run just client.", 
                    "Server + client / Just clent", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Question, 
                    MessageBoxDefaultButton.Button1))
            {
                server.Run();
            }

            // Show client window
            new ClientWindow().Show();
            new ClientWindow().Show();

            Application.Run();
        }
    }
}
