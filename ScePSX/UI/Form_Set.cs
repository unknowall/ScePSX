using System;
using System.IO;
using System.Windows.Forms;

namespace ScePSX.UI
{
    public partial class Form_Set : Form
    {
        private string id;
        private IniFile ini;

        public Form_Set(string id = "")
        {
            InitializeComponent();

            this.id = id;

            if (id == "")
            {
                loadini(FrmMain.ini);
                btndel.Visible = false;
            } else
            {
                string fn = $"./Save/{id}.ini";
                ini = new IniFile($"./Save/{id}.ini");
                if (!File.Exists(fn))
                {
                    loadini(FrmMain.ini);
                } else
                {
                    loadini(ini);
                }
                btndel.Visible = true;
                this.Text = $" {id} 的设置";
            }
        }

        private void btnsave_Click(object sender, EventArgs e)
        {
            if (id == "")
            {
                saveini(FrmMain.ini);
            } else
            {
                saveini(ini);
            }
        }

        private void btndel_Click(object sender, EventArgs e)
        {
            if (id == "")
                return;

            string fn = $"./Save/{id}.ini";
            if (File.Exists(fn))
            {
                File.Delete(fn);
            }
            loadini(FrmMain.ini);
        }

        private void loadini(IniFile ini)
        {
            tbcpusync.Text = ini.Read("CPU", "Sync");
            tbcyles.Text = ini.Read("CPU", "Cycles");
            tbframeidle.Text = ini.Read("CPU", "FrameIdle");
            tbframeskip.Text = ini.Read("Main", "SkipFrame");
            tbmipslock.Text = ini.Read("CPU", "MipsLock");
            tbaudiobuffer.Text = ini.Read("Audio", "Buffer");

            cbmsaa.SelectedIndex = ini.ReadInt("OpenGL", "MSAA");

            chkbios.Checked = ini.ReadInt("Main", "BiosDebug") == 1;
            chkcpu.Checked = ini.ReadInt("Main", "CPUDebug") == 1;
            chkTTY.Checked = ini.ReadInt("Main", "TTYDebug") == 1;
            cbconsole.Checked = ini.ReadInt("Main", "Console") == 1;

            var currbios = ini.Read("main", "bios");

            DirectoryInfo dir = new DirectoryInfo("./BIOS");
            if (dir.Exists)
            {
                if (dir.GetFiles().Length == 0)
                {
                    return;
                }
                cbbios.Items.Clear();
                foreach (FileInfo f in dir.GetFiles())
                {
                    cbbios.Items.Add(f.Name);
                    if (currbios == f.Name)
                        cbbios.SelectedIndex = cbbios.Items.Count - 1;
                }
            }
        }

        private void saveini(IniFile ini)
        {
            ini.WriteInt("CPU", "Sync", int.Parse(tbcpusync.Text));
            ini.WriteInt("CPU", "Cycles", int.Parse(tbcyles.Text));
            ini.WriteInt("CPU", "FrameIdle", int.Parse(tbframeidle.Text));
            ini.WriteInt("Main", "SkipFrame", int.Parse(tbframeskip.Text));
            ini.WriteInt("CPU", "MipsLock", int.Parse(tbmipslock.Text));
            ini.WriteInt("Audio", "Buffer", int.Parse(tbaudiobuffer.Text));

            ini.WriteInt("OpenGL", "MSAA", cbmsaa.SelectedIndex);

            ini.WriteInt("Main", "BiosDebug", chkbios.Checked ? 1 : 0);
            ini.WriteInt("Main", "CPUDebug", chkcpu.Checked ? 1 : 0);
            ini.WriteInt("Main", "TTYDebug", chkTTY.Checked ? 1 : 0);
            ini.WriteInt("Main", "Console", cbconsole.Checked ? 1 : 0);

            ini.Write("main", "bios", cbbios.Items[cbbios.SelectedIndex].ToString());
        }

    }
}
