using System.Windows.Forms;

namespace ScePSX.UI
{
    public partial class FrmAbout : Form
    {
        public FrmAbout()
        {
            InitializeComponent();

            labver.Text = FrmMain.version;

            label3.Text = $"{Translations.GetText("FrmAbout_memo1")}\r\n\r\n{Translations.GetText("FrmAbout_memo2")}\r\n";

            labSupport.Text = Translations.GetText("support_message");
        }

        private void Link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = linkLabel1.Text,
                    UseShellExecute = true
                });
            }
            catch
            {

            }
        }

        private void SupportLink_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = SupportLink.Text,
                    UseShellExecute = true
                });
            }
            catch
            {

            }
        }
    }
}
