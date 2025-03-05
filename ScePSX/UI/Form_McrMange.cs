using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScePSX;

namespace ScePSX.UI
{
    public partial class Form_McrMange : Form
    {
        MemCardMange card1, card2;
        ImageList imageList1, imageList2;

        public Form_McrMange()
        {
            InitializeComponent();
        }

        public Form_McrMange(string id) : this()
        {
            card1 = new MemCardMange($"./Save/{id}.dat");
            card2 = new MemCardMange("./Save/MemCard2.dat");
            InitializeListView();
            FillListView(lv1, card1, imageList1);
            FillListView(lv2, card2, imageList2);

            DirectoryInfo dir = new DirectoryInfo("./Save");
            if (dir.Exists)
            {
                foreach (FileInfo f in dir.GetFiles())
                {
                    if (Path.GetExtension(f.Name) == ".dat")
                    {
                        string name = Path.GetFileNameWithoutExtension(f.Name);
                        cbsave1.Items.Add(name);
                        cbsave2.Items.Add(name);
                        if (name == id)
                        {
                            cbsave1.SelectedIndex = cbsave1.Items.Count - 1;
                        }
                        if (name == "MemCard2")
                        {
                            cbsave2.SelectedIndex = cbsave2.Items.Count - 1;
                        }

                    }
                }
            }
        }

        private void InitializeListView()
        {
            imageList1 = new ImageList();
            imageList1.ImageSize = new Size(32, 32);
            lv1.SmallImageList = imageList1;
            lv1.Columns.Clear();
            lv1.Columns.Add("", 50);
            lv1.Columns.Add("Name", 250);

            imageList2 = new ImageList();
            imageList2.ImageSize = new Size(32, 32);
            lv2.SmallImageList = imageList2;
            lv2.Columns.Clear();
            lv2.Columns.Add("", 50);
            lv2.Columns.Add("Name", 250);

            cbsave1.DropDownStyle = ComboBoxStyle.DropDownList;
            cbsave2.DropDownStyle = ComboBoxStyle.DropDownList;

            cbsave1.SelectedIndexChanged += Cbsave1_SelectedIndexChanged;
            cbsave2.SelectedIndexChanged += Cbsave2_SelectedIndexChanged;
        }

        private void FillListView(ListView listView, MemCardMange card, ImageList imageList)
        {
            listView.Items.Clear();
            imageList.Images.Clear();
            for (int i = 0; i < MemCardMange.MaxSlot; i++)
            {
                var slot = card.Slots[i];
                if (slot != null && slot.type == MemCardMange.SlotTypes.initial)
                {
                    var item = new ListViewItem(i.ToString());
                    item.SubItems.Add(slot.Name);

                    Bitmap icon = slot.GetIconBitmap(0);
                    if (icon != null)
                    {
                        imageList.Images.Add(icon);
                        item.ImageIndex = imageList.Images.Count - 1;
                    }

                    listView.Items.Add(item);
                }
            }
        }

        private void Cbsave1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbsave1.SelectedIndex == -1)
                return;
            string selectedFile = cbsave1.SelectedItem.ToString();
            if (cbsave2.SelectedItem != null && selectedFile == cbsave2.SelectedItem.ToString())
            {
                MessageBox.Show("不能选择相同的文件，请选择其他文件。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cbsave1.SelectedIndex = -1;
                return;
            }
            card1 = new MemCardMange($"./Save/{selectedFile}.dat");
            FillListView(lv1, card1, imageList1);
        }

        private void Cbsave2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbsave2.SelectedIndex == -1)
                return;
            string selectedFile = cbsave2.SelectedItem.ToString();
            if (cbsave1.SelectedItem != null && selectedFile == cbsave1.SelectedItem.ToString())
            {
                MessageBox.Show("不能选择相同的文件，请选择其他文件。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cbsave2.SelectedIndex = -1;
                return;
            }
            card2 = new MemCardMange($"./Save/{selectedFile}.dat");
            FillListView(lv2, card2, imageList2);
        }

        private void move1to2_Click(object sender, EventArgs e)
        {
            if (lv1.SelectedItems.Count == 0)
                return;

            int slotNumber = int.Parse(lv1.SelectedItems[0].Text);
            byte[] saveBytes = card1.GetSaveBytes(slotNumber);
            if (card2.AddSaveBytes(slotNumber, saveBytes))
            {
                card1.DeleteSlot(slotNumber);
                FillListView(lv1, card1, imageList1);
                FillListView(lv2, card2, imageList2);
            } else
            {
                MessageBox.Show("无法移动存档，请检查目标存储卡是否有足够的空间。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void move2to1_Click(object sender, EventArgs e)
        {
            if (lv2.SelectedItems.Count == 0)
                return;

            int slotNumber = int.Parse(lv2.SelectedItems[0].Text);
            byte[] saveBytes = card2.GetSaveBytes(slotNumber);
            if (card1.AddSaveBytes(slotNumber, saveBytes))
            {
                card2.DeleteSlot(slotNumber);
                FillListView(lv1, card1, imageList1);
                FillListView(lv2, card2, imageList2);
            } else
            {
                MessageBox.Show("无法移动存档，请检查目标存储卡是否有足够的空间。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void del1_Click(object sender, EventArgs e)
        {
            if (lv1.SelectedItems.Count == 0)
                return;

            int slotNumber = int.Parse(lv1.SelectedItems[0].Text);
            card1.DeleteSlot(slotNumber);
            FillListView(lv1, card1, imageList1);
        }

        private void out1_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Memory Card (*.mcr)|*.mcr",
                FileName = "MemCard1.mcr"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                card1.SaveCard(saveFileDialog.FileName);
            }
        }

        private void save1_Click(object sender, EventArgs e)
        {
            string selectedFile = cbsave1.SelectedItem.ToString();
            card1.SaveCard($"./Save/{selectedFile}.dat");
        }

        private void del2_Click(object sender, EventArgs e)
        {
            if (lv2.SelectedItems.Count == 0)
                return;

            int slotNumber = int.Parse(lv2.SelectedItems[0].Text);
            card2.DeleteSlot(slotNumber);
            FillListView(lv2, card2, imageList2);
        }

        private void out2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Memory Card (*.mcr)|*.mcr",
                FileName = "MemCard2.mcr"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                card2.SaveCard(saveFileDialog.FileName);
            }
        }

        private void save2_Click(object sender, EventArgs e)
        {
            string selectedFile = cbsave2.SelectedItem.ToString();
            card2.SaveCard($"./Save/{selectedFile}.dat");
        }

        private void copy1to2_Click(object sender, EventArgs e)
        {
            if (lv1.SelectedItems.Count == 0)
                return;

            int slotNumber = int.Parse(lv1.SelectedItems[0].Text);
            byte[] saveBytes = card1.GetSaveBytes(slotNumber);
            if (card2.AddSaveBytes(slotNumber, saveBytes))
            {
                FillListView(lv2, card2, imageList2);
            } else
            {
                MessageBox.Show("无法复制存档，请检查目标存储卡是否有足够的空间。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void copy2to1_Click(object sender, EventArgs e)
        {
            if (lv2.SelectedItems.Count == 0)
                return;

            int slotNumber = int.Parse(lv2.SelectedItems[0].Text);
            byte[] saveBytes = card2.GetSaveBytes(slotNumber);
            if (card1.AddSaveBytes(slotNumber, saveBytes))
            {
                FillListView(lv1, card1, imageList1);
            } else
            {
                MessageBox.Show("无法复制存档，请检查目标存储卡是否有足够的空间。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
