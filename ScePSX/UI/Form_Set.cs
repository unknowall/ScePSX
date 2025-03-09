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
                this.Text = $" {id} {ScePSX.Properties.Resources.Form_Set_Form_Set_set}";
            }
        }

        private void edtxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
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
            tbbuscycles.Text = ini.Read("CPU", "BusCycles");
            tbcylesfix.Text = ini.Read("CPU", "CyclesFix");
            tbframeidle.Text = ini.Read("CPU", "FrameIdle");
            tbframeskip.Text = ini.Read("Main", "SkipFrame");
            tbcputicks.Text = ini.Read("CPU", "CpuTicks");
            tbaudiobuffer.Text = ini.Read("Audio", "Buffer");

            cbmsaa.SelectedIndex = ini.ReadInt("OpenGL", "MSAA");

            cbscalemode.SelectedIndex = ini.ReadInt("Main", "ScaleMode");

            chkbios.Checked = ini.ReadInt("Main", "BiosDebug") == 1;
            chkcpu.Checked = ini.ReadInt("Main", "CPUDebug") == 1;
            chkTTY.Checked = ini.ReadInt("Main", "TTYDebug") == 1;
            cbconsole.Checked = ini.ReadInt("Main", "Console") == 1;

            cbcpumode.SelectedIndex = ini.ReadInt("Main", "CpuMode");

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
            try
            {
                ini.WriteInt("CPU", "BusCycles", int.Parse(tbbuscycles.Text));
                ini.WriteInt("CPU", "CyclesFix", int.Parse(tbcylesfix.Text));
                ini.WriteFloat("CPU", "FrameIdle", double.Parse(tbframeidle.Text));
                ini.WriteInt("Main", "SkipFrame", int.Parse(tbframeskip.Text));
                ini.WriteInt("CPU", "CpuTicks", int.Parse(tbcputicks.Text));
                ini.WriteInt("Audio", "Buffer", int.Parse(tbaudiobuffer.Text));

                ini.WriteInt("OpenGL", "MSAA", cbmsaa.SelectedIndex);

                ini.WriteInt("Main", "BiosDebug", chkbios.Checked ? 1 : 0);
                ini.WriteInt("Main", "CPUDebug", chkcpu.Checked ? 1 : 0);
                ini.WriteInt("Main", "TTYDebug", chkTTY.Checked ? 1 : 0);
                ini.WriteInt("Main", "Console", cbconsole.Checked ? 1 : 0);

                ini.WriteInt("Main", "CpuMode", cbcpumode.SelectedIndex);

                ini.WriteInt("Main", "ScaleMode", cbscalemode.SelectedIndex);

                ini.Write("main", "bios", cbbios.Items[cbbios.SelectedIndex].ToString());
            } catch
            {
            }
        }

    }
}
