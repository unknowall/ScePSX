using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using ScePSX;

namespace ScePSX.UI
{
    public partial class Form_Cheat : Form
    {
        public List<PSXCore.CheatCode> cheatCodes = new List<PSXCore.CheatCode> { };

        private string DiskID;

        public Form_Cheat()
        {
            InitializeComponent();

            ctb.LostFocus += updatecodes;

            if (FrmMain.Core == null)
                return;
            DiskID = FrmMain.Core.DiskID;

            //DiskID = "SLPM_860.23";

            this.Text = "  " + DiskID + "  的金手指";

            btnload_Click(this, null);

            if (clb.Items.Count > 0)
                clb.Items[0].Selected = true;
        }

        private void btnadd_Click(object sender, EventArgs e)
        {
            var item = clb.Items.Add("新建项，请修改名称");
            item.SubItems.Add("");
        }

        private void btndel_Click(object sender, EventArgs e)
        {
            if (clb.SelectedItems.Count == 0)
                return;
            clb.Items.Remove(clb.SelectedItems[0]);
            ctb.Clear();
        }

        private void updatecodes(object sender, EventArgs e)
        {
            if (clb.SelectedItems.Count == 0)
                return;

            clb.SelectedItems[0].SubItems[1].Text = ctb.Text;
        }

        private void updateclbs()
        {
            ctb.Clear();
            clb.Items.Clear();
            foreach (var item in cheatCodes)
            {
                var clbitem = clb.Items.Add(item.Name);
                clbitem.Checked = item.Active;
                string codes = "";
                foreach (var sitem in item.Item)
                {
                    codes += $"{sitem.Address:X8} {sitem.Value:X4}\r\n";
                }
                clbitem.SubItems.Add(codes);
            }
        }

        private void btnload_Click(object sender, EventArgs e)
        {

            if (FrmMain.Core == null)
                return;

            cheatCodes.Clear();
            string fn = "./Cheats/" + DiskID + ".txt";
            if (!File.Exists(fn))
                return;
            cheatCodes = PSXCore.ParseTextToCheatCodeList(fn);

            updateclbs();
        }

        private void btnimp_Click(object sender, EventArgs e)
        {
            OpenFileDialog FD = new OpenFileDialog();

            FD.Filter = "CheatCodes|*.txt;*.cht";
            FD.ShowDialog();
            if (!File.Exists(FD.FileName) || FD.FileName == "")
                return;

            cheatCodes.Clear();
            try
            {
                cheatCodes = PSXCore.ParseTextToCheatCodeList(FD.FileName);
            } catch
            {
                return;
            }
            updateclbs();
        }

        private string GetText()
        {
            string ret = "";
            for (int i = 0; i < clb.Items.Count; i++)
            {
                var item = clb.Items[i];
                ret += "\r\n[" + item.Text + "]\r\n";
                ret += "Active = ";
                if (item.Checked)
                    ret += "1\r\n";
                else
                    ret += "0\r\n";
                ret += item.SubItems[1].Text;
            }
            return ret;
        }

        private void btnapply_Click(object sender, EventArgs e)
        {
            if (FrmMain.Core == null)
                return;

            btnsave_Click(sender, e);

            FrmMain.Core.LoadCheats();
        }

        private void btnsave_Click(object sender, EventArgs e)
        {
            string fn = "./Cheats/" + DiskID + ".txt";
            string txt = GetText();

            File.WriteAllText(fn, txt);
        }

        private void clb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (clb.SelectedItems.Count == 0)
                return;

            ctb.Text = "";
            if (clb.SelectedItems[0].SubItems.Count >= 2)
            {
                var sub = clb.SelectedItems[0].SubItems[1];
                ctb.Text += sub.Text;
            }
        }
    }

}
