using System.Windows.Forms;

namespace ScePSX.UI
{
    public partial class FrmAbout : Form
    {
        public FrmAbout()
        {
            InitializeComponent();

            labver.Text = FrmMain.version;

            label3.Text = $"{ScePSX.Properties.Resources.FrmAbout_InitializeComponent_read}\r\n\r\n{ScePSX.Properties.Resources.FrmAbout_InitializeComponent_read2}\r\n";

            labSupport.Text = ScePSX.Properties.Resources.FrmAbout_FrmAbout_support;
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
            } catch
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
            } catch
            {

            }
        }
    }
}
