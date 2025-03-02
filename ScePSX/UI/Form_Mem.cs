using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HexBoxControl;

using ScePSX;

namespace ScePSX.UI
{
    public partial class Form_Mem : Form
    {
        private const long PSX_BASE = 0x80000000;

        private byte[] blankdata = new byte[1024];

        private MemorySearch memsearch;

        private static List<(int Address, object Value)> SearchResults = new List<(int Address, object Value)> { };

        public unsafe Form_Mem()
        {
            InitializeComponent();

            CboView.Items.AddRange(Enum.GetNames(typeof(HexBoxViewMode)));
            CboView.SelectedIndex = 2;

            CboEncode.Items.AddRange
            (
                new object[]
                {
                    new AnsiCharConvertor(),
                    new AsciiCharConvertor(),
                    new Utf8CharConvertor()
                }
            );
            CboEncode.SelectedIndex = 0;

            HexBox.ResetOffset = false;
            HexBox.AddressOffset = PSX_BASE;

            if (FrmMain.Core != null)
            {
                HexBox.Dump = ConvertBytePointerToByteArray(FrmMain.Core.PsxBus.ramPtr, 2048 * 1024);
            } else
            {
                HexBox.Dump = blankdata;
            }

            updateml();

            System.Windows.Forms.Timer sdlEventTimer = new System.Windows.Forms.Timer();
            sdlEventTimer.Interval = 1000;
            sdlEventTimer.Tick += updtimer;
            sdlEventTimer.Start();
        }

        private unsafe byte[] ConvertBytePointerToByteArray(byte* ptr, int length)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = *(ptr + i);
            }
            return result;
        }

        private void CboView_SelectedIndexChanged(object sender, EventArgs e)
        {
            HexBoxViewMode mode;
            Enum.TryParse(CboView.SelectedItem.ToString(), out mode);
            HexBox.ViewMode = mode;
        }

        private void CboEncode_SelectedIndexChanged(object sender, EventArgs e)
        {
            HexBox.CharConverter = CboEncode.SelectedItem as ICharConverter;
        }

        private void tbgoto_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                btngo_Click(sender, e);
        }

        private unsafe void updtimer(object sender, EventArgs e)
        {
            if (!chkupd.Checked || FrmMain.Core == null)
                return;
            HexBox.Dump = ConvertBytePointerToByteArray(FrmMain.Core.PsxBus.ramPtr, 2048 * 1024);
        }

        private unsafe void btnupd_Click(object sender, EventArgs e)
        {
            if (FrmMain.Core == null)
                return;
            HexBox.Dump = ConvertBytePointerToByteArray(FrmMain.Core.PsxBus.ramPtr, 2048 * 1024);
        }

        private unsafe void HexBox_Edited(object sender, HexBoxEditEventArgs e)
        {
            if (FrmMain.Core == null || e.NewValue == e.OldValue)
                return;

            FrmMain.Core.PsxBus.ramPtr[e.Offset] = (byte)e.NewValue;
        }

        private void btngo_Click(object sender, EventArgs e)
        {
            long pos = 0;
            try
            {
                pos = Convert.ToInt32(tbgoto.Text, 16);
            } catch
            {
                return;
            }

            if (pos < PSX_BASE)
                pos = pos + PSX_BASE;

            HexBox.ScrollTo(pos);
        }

        private void ml_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            //switch (this.ml.Columns[e.ColumnIndex].Name)
            //{
            //    case "address":
            //        e.Value = (PSX_BASE + SearchResults[e.RowIndex].Address).ToString("X8");
            //        break;

            //    case "val":
            //        e.Value = SearchResults[e.RowIndex].Value;
            //        break;
            //}
        }

        private void ml_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > SearchResults.Count)
                return;

            long pos = SearchResults[e.RowIndex].Address + PSX_BASE;

            HexBox.ScrollTo(pos);
        }

        private unsafe void ml_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (this.ml.Columns[e.ColumnIndex].Name == "val")
            {

                SearchResults[e.RowIndex] = (SearchResults[e.RowIndex].Address, this.ml.Rows[e.RowIndex].Cells[1].Value);

                uint tmp = uint.Parse(SearchResults[e.RowIndex].Value.ToString());
                uint adr = (uint)SearchResults[e.RowIndex].Address;

                adr = FrmMain.Core.PsxBus.GetMask(adr);
                if (tmp < 0xFF)
                {
                    FrmMain.Core.PsxBus.write(adr & 0x1F_FFFF, (byte)tmp, FrmMain.Core.PsxBus.ramPtr);
                } else if (tmp < 0xFFFF)
                {
                    FrmMain.Core.PsxBus.write(adr & 0x1F_FFFF, (ushort)tmp, FrmMain.Core.PsxBus.ramPtr);
                } else
                {
                    FrmMain.Core.PsxBus.write(adr & 0x1F_FFFF, tmp, FrmMain.Core.PsxBus.ramPtr);
                }
            }
        }

        private void findb_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }
        }

        private unsafe void btnr_Click(object sender, EventArgs e)
        {
            if (FrmMain.Core == null)
                return;

            memsearch = new MemorySearch(ConvertBytePointerToByteArray(FrmMain.Core.PsxBus.ramPtr, 2048 * 1024));

            SearchResults.Clear();

            updateml();
        }

        private unsafe void btns_Click(object sender, EventArgs e)
        {
            if (FrmMain.Core == null)
                return;

            if (memsearch == null)
                memsearch = new MemorySearch(ConvertBytePointerToByteArray(FrmMain.Core.PsxBus.ramPtr, 2048 * 1024));
            else
                memsearch.UpdateData(ConvertBytePointerToByteArray(FrmMain.Core.PsxBus.ramPtr, 2048 * 1024));

            if (rbbyte.Checked)
            {
                byte tmp;
                if (!byte.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchByte(tmp);
            } else
            if (rbWord.Checked)
            {
                ushort tmp;
                if (!ushort.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchWord(tmp);
            } else
            if (rbDword.Checked)
            {
                uint tmp;
                if (!uint.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchDword(tmp);
            } else
            if (rbfloat.Checked)
            {
                float tmp;
                if (!float.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchFloat(tmp);
            }

            SearchResults = memsearch.GetResults();

            updateml();
        }

        private void updateml()
        {
            labse.Text = $"搜索到 {SearchResults.Count} 个地址  (只显示前500个)";
            ml.Rows.Clear();
            for (int i = 0; i < SearchResults.Count; i++)
            {
                if (i >= 500)
                    break;

                ml.Rows.Add((PSX_BASE + SearchResults[i].Address).ToString("X8"), SearchResults[i].Value);
            }
        }

    }

}
