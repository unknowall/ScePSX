using Kaillera;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using static Kaillera.KailleraClient;
using static Kaillera.KailleraHandler;

namespace ScePSX.Win.UI
{
    public partial class Form_Kaillera : Form
    {
        KailleraHandler Kaillera;

        public Form_Kaillera()
        {
            InitializeComponent();

            Kaillera = new KailleraHandler();

            Kaillera.OnEvent += OnEvent;
        }

        private void OnEvent(Events events, string Msg)
        {
            switch (events)
            {
                case KailleraHandler.Events.Connect:
                    FillGames();
                    Status.Items[0].Text = $"Connected...";
                    break;
                case KailleraHandler.Events.Disconnect:
                    FillGames();
                    Status.Items[0].Text = $"Disconnect...";
                    break;
                case KailleraHandler.Events.ConnectFailed:
                    FillGames();
                    Status.Items[0].Text = $"ConnectFailed: {Msg}";
                    break;
                case KailleraHandler.Events.SrvMsg:
                    tbChat.Text += Msg + "\n";
                    tbChat.SelectionStart = tbChat.Text.Length;
                    tbChat.ScrollToCaret();
                    break;
                case KailleraHandler.Events.ChatMsg:
                    tbChat.Text += Msg + "\n";
                    tbChat.SelectionStart = tbChat.Text.Length;
                    tbChat.ScrollToCaret();
                    break;
            }
        }

        private void Form_Kaillera_Load(object sender, EventArgs e)
        {

        }

        private void Form_Kaillera_Shown(object sender, EventArgs e)
        {
            Status.Items[0].Text = "Fetch ServerList...";
            btnRefrsh_Click(sender, e);
        }

        private async void btnRefrsh_Click(object sender, EventArgs e)
        {
            await Kaillera.Client.FetchServerList();

            UpdateSrv();

            PingSrvs();

            Status.Items[0].Text = $"Fetched {Kaillera.Client.Servers.Count} Server";
        }

        private void FillGames()
        {
            lvGames.Items.Clear();
            foreach (var game in Kaillera.Client.serverGames)
            {
                //if (game.EmulatorName != "ScePSX")
                //    continue;

                var item = lvGames.Items.Add(game.GameName);
                item.SubItems.Add(game.Owner);
                item.SubItems.Add($"{game.PlayerCount} / {game.MaxPlayers}");
                item.Tag = game;
            }
        }

        private void UpdateSrv()
        {
            lvSrv.Items.Clear();
            foreach (var srv in Kaillera.Client.Servers)
            {
                var item = lvSrv.Items.Add("");
                item.SubItems.Add(srv.Name);
                item.SubItems.Add(srv.Location);
                item.SubItems.Add($"{srv.Users} / {srv.MaxUsers}");
                item.Tag = srv;
            }
        }

        private async void PingSrvs()
        {
            foreach (var srv in lvSrv.Items)
            {
                var srvItem = (srv as ListViewItem);
                var srvip = Kaillera.Client.Servers[srvItem.Index].Address;
                var ping = await Kaillera.Client.PingAsync(srvip);
                srvItem.Text = ping.ToString();
            }
        }

        private void lvSrv_DoubleClick(object sender, EventArgs e)
        {
            var item = lvSrv.SelectedItems[0];
            if (item == null)
                return;

            ServerInfo srv = (ServerInfo)item.Tag;

            Status.Items[0].Text = $"Connect To {srv.Name}";

            var ret = Kaillera.Client.Connect(srv.Address, srv.Port, edUserName.Text);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            var addr = edAddress.Text.Split(":")[0];
            var port = edAddress.Text.Split(":")[1];

            Status.Items[0].Text = $"Connect To {edAddress.Text}";

            var ret = Kaillera.Client.Connect(addr, Convert.ToInt32(port), edUserName.Text);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (Kaillera.Client.connected)
                Kaillera.Client.SendGlobalChat(edChat.Text);
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {

        }

        private void btnJoin_Click(object sender, EventArgs e)
        {

        }
    }
}
