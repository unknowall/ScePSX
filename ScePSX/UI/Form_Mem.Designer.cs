using System.Windows.Forms;

namespace ScePSX.UI
{
    partial class Form_Mem
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            var ansiCharConvertor1 = new HexBoxControl.AnsiCharConvertor();
            ml = new DataGridView();
            address = new DataGridViewTextBoxColumn();
            val = new DataGridViewTextBoxColumn();
            splitContainer1 = new SplitContainer();
            CboEncode = new ComboBox();
            CboView = new ComboBox();
            HexBox = new HexBoxControl.HexBox();
            btnupd = new Button();
            chkupd = new CheckBox();
            btngo = new Button();
            tbgoto = new TextBox();
            label1 = new Label();
            label2 = new Label();
            btns = new Button();
            btnr = new Button();
            findb = new TextBox();
            gbst = new GroupBox();
            rbfloat = new RadioButton();
            rbDword = new RadioButton();
            rbWord = new RadioButton();
            rbbyte = new RadioButton();
            labse = new Label();
            ((System.ComponentModel.ISupportInitialize)ml).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            gbst.SuspendLayout();
            SuspendLayout();
            // 
            // ml
            // 
            ml.AllowUserToAddRows = false;
            ml.AllowUserToDeleteRows = false;
            ml.AllowUserToResizeRows = false;
            ml.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            ml.Columns.AddRange(new DataGridViewColumn[] { address, val });
            ml.Location = new System.Drawing.Point(7, 214);
            ml.MultiSelect = false;
            ml.Name = "ml";
            ml.RowHeadersVisible = false;
            ml.ScrollBars = ScrollBars.Vertical;
            ml.ShowCellErrors = false;
            ml.ShowCellToolTips = false;
            ml.ShowEditingIcon = false;
            ml.ShowRowErrors = false;
            ml.Size = new System.Drawing.Size(290, 306);
            ml.TabIndex = 33;
            ml.CellDoubleClick += ml_CellDoubleClick;
            ml.CellEndEdit += ml_CellEndEdit;
            // 
            // address
            // 
            address.HeaderText = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_地址;
            address.MinimumWidth = 160;
            address.Name = "address";
            address.ReadOnly = true;
            address.Width = 160;
            // 
            // val
            // 
            val.HeaderText = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_数值;
            val.MinimumWidth = 100;
            val.Name = "val";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(CboEncode);
            splitContainer1.Panel1.Controls.Add(CboView);
            splitContainer1.Panel1.Controls.Add(HexBox);
            splitContainer1.Panel1.Controls.Add(btnupd);
            splitContainer1.Panel1.Controls.Add(chkupd);
            splitContainer1.Panel1.Controls.Add(btngo);
            splitContainer1.Panel1.Controls.Add(tbgoto);
            splitContainer1.Panel1.Controls.Add(label1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(label2);
            splitContainer1.Panel2.Controls.Add(btns);
            splitContainer1.Panel2.Controls.Add(btnr);
            splitContainer1.Panel2.Controls.Add(findb);
            splitContainer1.Panel2.Controls.Add(gbst);
            splitContainer1.Panel2.Controls.Add(labse);
            splitContainer1.Panel2.Controls.Add(ml);
            splitContainer1.Size = new System.Drawing.Size(897, 532);
            splitContainer1.SplitterDistance = 590;
            splitContainer1.TabIndex = 27;
            // 
            // CboEncode
            // 
            CboEncode.DropDownStyle = ComboBoxStyle.DropDownList;
            CboEncode.FormattingEnabled = true;
            CboEncode.Location = new System.Drawing.Point(341, 5);
            CboEncode.Name = "CboEncode";
            CboEncode.Size = new System.Drawing.Size(69, 25);
            CboEncode.TabIndex = 8;
            CboEncode.SelectedIndexChanged += CboEncode_SelectedIndexChanged;
            // 
            // CboView
            // 
            CboView.DropDownStyle = ComboBoxStyle.DropDownList;
            CboView.FormattingEnabled = true;
            CboView.Location = new System.Drawing.Point(251, 5);
            CboView.Name = "CboView";
            CboView.Size = new System.Drawing.Size(84, 25);
            CboView.TabIndex = 7;
            CboView.SelectedIndexChanged += CboView_SelectedIndexChanged;
            // 
            // HexBox
            // 
            HexBox.AddressOffset = 0L;
            HexBox.CharConverter = ansiCharConvertor1;
            HexBox.Columns = 16;
            HexBox.ColumnsAuto = false;
            HexBox.Dock = DockStyle.Bottom;
            HexBox.Dump = null;
            HexBox.Font = new System.Drawing.Font("Tahoma", 10.5F);
            HexBox.Location = new System.Drawing.Point(0, 34);
            HexBox.Name = "HexBox";
            HexBox.ResetOffset = false;
            HexBox.Size = new System.Drawing.Size(590, 498);
            HexBox.TabIndex = 6;
            HexBox.ViewMode = HexBoxControl.HexBoxViewMode.BytesAscii;
            HexBox.Edited += HexBox_Edited;
            // 
            // btnupd
            // 
            btnupd.Location = new System.Drawing.Point(497, 5);
            btnupd.Name = "btnupd";
            btnupd.Size = new System.Drawing.Size(70, 23);
            btnupd.TabIndex = 5;
            btnupd.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_刷新;
            btnupd.UseVisualStyleBackColor = true;
            btnupd.Click += btnupd_Click;
            // 
            // chkupd
            // 
            chkupd.AutoSize = true;
            chkupd.Location = new System.Drawing.Point(416, 8);
            chkupd.Name = "chkupd";
            chkupd.Size = new System.Drawing.Size(75, 21);
            chkupd.TabIndex = 4;
            chkupd.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_自动刷新;
            chkupd.UseVisualStyleBackColor = true;
            // 
            // btngo
            // 
            btngo.Location = new System.Drawing.Point(162, 5);
            btngo.Name = "btngo";
            btngo.Size = new System.Drawing.Size(75, 23);
            btngo.TabIndex = 3;
            btngo.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_前往地址;
            btngo.UseVisualStyleBackColor = true;
            btngo.Click += btngo_Click;
            // 
            // tbgoto
            // 
            tbgoto.Location = new System.Drawing.Point(32, 5);
            tbgoto.Name = "tbgoto";
            tbgoto.Size = new System.Drawing.Size(119, 23);
            tbgoto.TabIndex = 2;
            tbgoto.KeyPress += tbgoto_KeyPress;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(21, 17);
            label1.TabIndex = 1;
            label1.Text = "0x";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Microsoft YaHei UI", 13F, System.Drawing.FontStyle.Bold);
            label2.Location = new System.Drawing.Point(8, 12);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(102, 25);
            label2.TabIndex = 43;
            label2.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_内存搜索;
            // 
            // btns
            // 
            btns.Location = new System.Drawing.Point(212, 44);
            btns.Name = "btns";
            btns.Size = new System.Drawing.Size(75, 26);
            btns.TabIndex = 42;
            btns.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_搜索;
            btns.UseVisualStyleBackColor = true;
            btns.Click += btns_Click;
            // 
            // btnr
            // 
            btnr.Location = new System.Drawing.Point(212, 12);
            btnr.Name = "btnr";
            btnr.Size = new System.Drawing.Size(75, 23);
            btnr.TabIndex = 41;
            btnr.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_重置;
            btnr.UseVisualStyleBackColor = true;
            btnr.Click += btnr_Click;
            // 
            // findb
            // 
            findb.Location = new System.Drawing.Point(13, 45);
            findb.Name = "findb";
            findb.Size = new System.Drawing.Size(186, 23);
            findb.TabIndex = 40;
            findb.KeyPress += findb_KeyPress;
            // 
            // gbst
            // 
            gbst.Controls.Add(rbfloat);
            gbst.Controls.Add(rbDword);
            gbst.Controls.Add(rbWord);
            gbst.Controls.Add(rbbyte);
            gbst.Location = new System.Drawing.Point(8, 80);
            gbst.Name = "gbst";
            gbst.Size = new System.Drawing.Size(289, 100);
            gbst.TabIndex = 39;
            gbst.TabStop = false;
            gbst.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_搜索类型;
            // 
            // rbfloat
            // 
            rbfloat.AutoSize = true;
            rbfloat.Location = new System.Drawing.Point(161, 59);
            rbfloat.Name = "rbfloat";
            rbfloat.Size = new System.Drawing.Size(86, 21);
            rbfloat.TabIndex = 42;
            rbfloat.TabStop = true;
            rbfloat.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_浮点Float;
            rbfloat.UseVisualStyleBackColor = true;
            // 
            // rbDword
            // 
            rbDword.AutoSize = true;
            rbDword.Location = new System.Drawing.Point(37, 59);
            rbDword.Name = "rbDword";
            rbDword.Size = new System.Drawing.Size(106, 21);
            rbDword.TabIndex = 41;
            rbDword.TabStop = true;
            rbDword.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_双字DWORD;
            rbDword.UseVisualStyleBackColor = true;
            // 
            // rbWord
            // 
            rbWord.AutoSize = true;
            rbWord.Location = new System.Drawing.Point(161, 32);
            rbWord.Name = "rbWord";
            rbWord.Size = new System.Drawing.Size(85, 21);
            rbWord.TabIndex = 40;
            rbWord.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_字WORD;
            rbWord.UseVisualStyleBackColor = true;
            // 
            // rbbyte
            // 
            rbbyte.AutoSize = true;
            rbbyte.Checked = true;
            rbbyte.Location = new System.Drawing.Point(36, 32);
            rbbyte.Name = "rbbyte";
            rbbyte.Size = new System.Drawing.Size(83, 21);
            rbbyte.TabIndex = 39;
            rbbyte.TabStop = true;
            rbbyte.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_字节Byte;
            rbbyte.UseVisualStyleBackColor = true;
            // 
            // labse
            // 
            labse.AutoSize = true;
            labse.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold);
            labse.Location = new System.Drawing.Point(7, 191);
            labse.Name = "labse";
            labse.Size = new System.Drawing.Size(225, 19);
            labse.TabIndex = 34;
            labse.Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_搜索到0个地址只显示前500个;
            // 
            // Form_Mem
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(897, 532);
            Controls.Add(splitContainer1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            KeyPreview = true;
            MaximizeBox = false;
            Name = "Form_Mem";
            StartPosition = FormStartPosition.CenterScreen;
            Text = ScePSX.Properties.Resources.Form_Mem_InitializeComponent_内存编辑;
            ((System.ComponentModel.ISupportInitialize)ml).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            gbst.ResumeLayout(false);
            gbst.PerformLayout();
            ResumeLayout(false);

        }

        #endregion
        private DataGridView ml;
        private SplitContainer splitContainer1;
        private GroupBox gbst;
        private RadioButton rbfloat;
        private RadioButton rbDword;
        private RadioButton rbWord;
        private RadioButton rbbyte;
        private Label labse;
        private Label label2;
        private Button btns;
        private Button btnr;
        private TextBox findb;
        private DataGridViewTextBoxColumn address;
        private DataGridViewTextBoxColumn val;
        private Button btngo;
        private TextBox tbgoto;
        private Label label1;
        private Button btnupd;
        private CheckBox chkupd;
        private HexBoxControl.HexBox HexBox;
        private ComboBox CboEncode;
        private ComboBox CboView;
    }
}
