using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScePSX.UI
{
    public partial class Form_Set : Form
    {
        public Form_Set()
        {
            InitializeComponent();

            tbcpusync.Text = FrmMain.ini.Read("CPU", "Sync");
            tbcyles.Text = FrmMain.ini.Read("CPU", "Cycles");
            tbframeidle.Text = FrmMain.ini.Read("CPU", "FrameIdle");
            tbframeskip.Text = FrmMain.ini.Read("Main", "SkipFrame");
            tbmipslock.Text = FrmMain.ini.Read("CPU", "MipsLock");
            tbaudiobuffer.Text = FrmMain.ini.Read("Audio", "Buffer");

            cbmsaa.SelectedIndex = FrmMain.ini.ReadInt("OpenGL", "MSAA");

            chkbios.Checked = FrmMain.ini.ReadInt("Main", "BiosDebug") == 1;
            chkcpu.Checked = FrmMain.ini.ReadInt("Main", "CPUDebug") == 1;
            chkTTY.Checked = FrmMain.ini.ReadInt("Main", "TTYDebug") == 1;
            cbconsole.Checked = FrmMain.ini.ReadInt("Main", "Console") == 1;
        }

        private void btnsave_Click(object sender, EventArgs e)
        {
            FrmMain.ini.WriteInt("CPU", "Sync", int.Parse(tbcpusync.Text));
            FrmMain.ini.WriteInt("CPU", "Cycles", int.Parse(tbcyles.Text));
            FrmMain.ini.WriteInt("CPU", "FrameIdle", int.Parse(tbframeidle.Text));
            FrmMain.ini.WriteInt("Main", "SkipFrame", int.Parse(tbframeskip.Text));
            FrmMain.ini.WriteInt("CPU", "MipsLock", int.Parse(tbmipslock.Text));
            FrmMain.ini.WriteInt("Audio", "Buffer", int.Parse(tbaudiobuffer.Text));

            FrmMain.ini.WriteInt("OpenGL", "MSAA", cbmsaa.SelectedIndex);

            FrmMain.ini.WriteInt("Main", "BiosDebug", chkbios.Checked ? 1 : 0);
            FrmMain.ini.WriteInt("Main", "CPUDebug", chkcpu.Checked ? 1 : 0);
            FrmMain.ini.WriteInt("Main", "TTYDebug", chkTTY.Checked ? 1 : 0);
            FrmMain.ini.WriteInt("Main", "Console", cbconsole.Checked ? 1 : 0);
        }
    }
}
