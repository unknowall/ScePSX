using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;

namespace ScePSX.UI
{
    public partial class FrmNetPlay : Form
    {
        public FrmNetPlay()
        {
            InitializeComponent();

            tblocalip.Text = GetSuitableIPv4Address();

            tbsrvip.Text = tblocalip.Text;
        }

        public string GetSuitableIPv4Address()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            var activeInterfaces = networkInterfaces
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                              !IsVirtualNetworkInterface(nic))
                .OrderByDescending(nic => nic.Speed)
                .ToList();

            foreach (var nic in activeInterfaces)
            {
                var properties = nic.GetIPProperties();
                var ipv4Addresses = properties.UnicastAddresses
                    .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                                   !IPAddress.IsLoopback(addr.Address))
                    .Select(addr => addr.Address)
                    .ToList();

                if (ipv4Addresses.Any())
                {
                    return ipv4Addresses.First().ToString();
                }
            }

            return null;
        }

        static bool IsVirtualNetworkInterface(NetworkInterface nic)
        {
            string[] virtualKeywords = { "VMware", "VirtualBox", "Hyper-V", "TAP-Windows", "vEthernet" };

            string description = nic.Description?.ToLowerInvariant() ?? "";
            string name = nic.Name?.ToLowerInvariant() ?? "";

            return virtualKeywords.Any(keyword => description.Contains(keyword.ToLowerInvariant()) ||
                                                   name.Contains(keyword.ToLowerInvariant()));
        }

        private void btncli_Click(object sender, EventArgs e)
        {
            if (FrmMain.Core == null)
                return;

            FrmMain.Core.PsxBus.SIO.Close();
            FrmMain.Core.PsxBus.SIO.Active(false, tblocalip.Text, tbsrvip.Text);

            labhint.Text = ScePSX.Properties.Resources.FrmNetPlay_btncli_Click_已启用客户机模式;
        }

        private void btnsrv_Click(object sender, EventArgs e)
        {
            if (FrmMain.Core == null)
                return;

            FrmMain.Core.PsxBus.SIO.Close();
            FrmMain.Core.PsxBus.SIO.Active(true, tblocalip.Text, tbsrvip.Text);

            labhint.Text = ScePSX.Properties.Resources.FrmNetPlay_btnsrv_Click_已启用主机模式;
        }
    }
}
