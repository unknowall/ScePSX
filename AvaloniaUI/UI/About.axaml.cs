using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;

namespace ScePSX.UI
{
    public partial class AboutFrm : Window
    {
        public AboutFrm()
        {
            InitializeComponent();

            labver.Text = MainWindow.version;

            labinfo.Text = Translations.GetText("FrmAbout_memo1");

            labSupport.Text = Translations.GetText("support_message");
        }

        private void Link_LinkClicked(object sender, PointerPressedEventArgs e)
        {
            OpenUrl("https://github.com/unknowall/ScePSX");
        }

        private void SupportLink_Click(object sender, PointerPressedEventArgs e)
        {
            OpenUrl("https://ko-fi.com/unknowall");
        }

        private void OpenUrl(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            } catch (Exception ex)
            {
                Console.WriteLine($"Failed to open URL: {ex.Message}");
            }
        }
    }
}
