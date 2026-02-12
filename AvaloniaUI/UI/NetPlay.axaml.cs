using System.Net;
using System.Net.Sockets;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ScePSX.UI
{
    public partial class NetPlayFrm : Window
    {
        PSXCore? Core;

        public NetPlayFrm(PSXCore? core)
        {
            InitializeComponent();

            tblocalip.Text = GetLocalIPAddress();

            Core = core;
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            } catch { }
            return "127.0.0.1";
        }

        private void btnsrv_Click(object sender, RoutedEventArgs e)
        {
            // 作为主机启动
            string localIP = tblocalip.Text ?? "";
            if (string.IsNullOrWhiteSpace(localIP))
            {
                labnethint.Text = "❌ 请输入有效的IP地址";
                return;
            }

            labnethint.Text = "✅ 主机模式已启动，等待连接...";

            Core.PsxBus.SIO.Close();
            Core.PsxBus.SIO.Active(true, tblocalip.Text, tbsrvip.Text);
        }

        private void btncli_Click(object sender, RoutedEventArgs e)
        {
            // 作为客户机启动
            string hostIP = tbsrvip.Text ?? "";
            if (string.IsNullOrWhiteSpace(hostIP))
            {
                labnethint.Text = "❌ 请输入目标主机IP地址";
                return;
            }

            labnethint.Text = $"🔄 正在连接到 {hostIP}...";

            Core.PsxBus.SIO.Close();
            Core.PsxBus.SIO.Active(false, tblocalip.Text, tbsrvip.Text);
        }
    }
}
