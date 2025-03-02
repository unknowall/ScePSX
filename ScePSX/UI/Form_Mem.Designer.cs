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
            HexBoxControl.AnsiCharConvertor ansiCharConvertor1 = new HexBoxControl.AnsiCharConvertor();
            this.ml = new System.Windows.Forms.DataGridView();
            this.address = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.val = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.CboEncode = new System.Windows.Forms.ComboBox();
            this.CboView = new System.Windows.Forms.ComboBox();
            this.HexBox = new HexBoxControl.HexBox();
            this.btnupd = new System.Windows.Forms.Button();
            this.chkupd = new System.Windows.Forms.CheckBox();
            this.btngo = new System.Windows.Forms.Button();
            this.tbgoto = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btns = new System.Windows.Forms.Button();
            this.btnr = new System.Windows.Forms.Button();
            this.findb = new System.Windows.Forms.TextBox();
            this.gbst = new System.Windows.Forms.GroupBox();
            this.rbfloat = new System.Windows.Forms.RadioButton();
            this.rbDword = new System.Windows.Forms.RadioButton();
            this.rbWord = new System.Windows.Forms.RadioButton();
            this.rbbyte = new System.Windows.Forms.RadioButton();
            this.labse = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.ml)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.gbst.SuspendLayout();
            this.SuspendLayout();
            // 
            // ml
            // 
            this.ml.AllowUserToAddRows = false;
            this.ml.AllowUserToDeleteRows = false;
            this.ml.AllowUserToResizeRows = false;
            this.ml.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ml.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.address,
            this.val});
            this.ml.Location = new System.Drawing.Point(7, 214);
            this.ml.MultiSelect = false;
            this.ml.Name = "ml";
            this.ml.RowHeadersVisible = false;
            this.ml.RowTemplate.Height = 25;
            this.ml.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ml.ShowCellErrors = false;
            this.ml.ShowCellToolTips = false;
            this.ml.ShowEditingIcon = false;
            this.ml.ShowRowErrors = false;
            this.ml.Size = new System.Drawing.Size(290, 306);
            this.ml.TabIndex = 33;
            this.ml.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ml_CellDoubleClick);
            this.ml.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.ml_CellEndEdit);
            // 
            // address
            // 
            this.address.HeaderText = "地址";
            this.address.MinimumWidth = 160;
            this.address.Name = "address";
            this.address.ReadOnly = true;
            this.address.Width = 160;
            // 
            // val
            // 
            this.val.HeaderText = "数值";
            this.val.MinimumWidth = 100;
            this.val.Name = "val";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.CboEncode);
            this.splitContainer1.Panel1.Controls.Add(this.CboView);
            this.splitContainer1.Panel1.Controls.Add(this.HexBox);
            this.splitContainer1.Panel1.Controls.Add(this.btnupd);
            this.splitContainer1.Panel1.Controls.Add(this.chkupd);
            this.splitContainer1.Panel1.Controls.Add(this.btngo);
            this.splitContainer1.Panel1.Controls.Add(this.tbgoto);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.label2);
            this.splitContainer1.Panel2.Controls.Add(this.btns);
            this.splitContainer1.Panel2.Controls.Add(this.btnr);
            this.splitContainer1.Panel2.Controls.Add(this.findb);
            this.splitContainer1.Panel2.Controls.Add(this.gbst);
            this.splitContainer1.Panel2.Controls.Add(this.labse);
            this.splitContainer1.Panel2.Controls.Add(this.ml);
            this.splitContainer1.Size = new System.Drawing.Size(987, 532);
            this.splitContainer1.SplitterDistance = 673;
            this.splitContainer1.TabIndex = 27;
            // 
            // CboEncode
            // 
            this.CboEncode.FormattingEnabled = true;
            this.CboEncode.Location = new System.Drawing.Point(379, 5);
            this.CboEncode.Name = "CboEncode";
            this.CboEncode.Size = new System.Drawing.Size(121, 25);
            this.CboEncode.TabIndex = 8;
            this.CboEncode.SelectedIndexChanged += new System.EventHandler(this.CboEncode_SelectedIndexChanged);
            // 
            // CboView
            // 
            this.CboView.FormattingEnabled = true;
            this.CboView.Location = new System.Drawing.Point(251, 5);
            this.CboView.Name = "CboView";
            this.CboView.Size = new System.Drawing.Size(113, 25);
            this.CboView.TabIndex = 7;
            this.CboView.SelectedIndexChanged += new System.EventHandler(this.CboView_SelectedIndexChanged);
            // 
            // HexBox
            // 
            this.HexBox.AddressOffset = ((long)(0));
            this.HexBox.CharConverter = ansiCharConvertor1;
            this.HexBox.Columns = 16;
            this.HexBox.ColumnsAuto = false;
            this.HexBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.HexBox.Dump = null;
            this.HexBox.Font = new System.Drawing.Font("Tahoma", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.HexBox.Location = new System.Drawing.Point(0, 34);
            this.HexBox.Name = "HexBox";
            this.HexBox.ResetOffset = false;
            this.HexBox.Size = new System.Drawing.Size(673, 498);
            this.HexBox.TabIndex = 6;
            this.HexBox.ViewMode = HexBoxControl.HexBoxViewMode.BytesAscii;
            this.HexBox.Edited += new HexBoxControl.HexBoxEditEventHandler(this.HexBox_Edited);
            // 
            // btnupd
            // 
            this.btnupd.Location = new System.Drawing.Point(600, 5);
            this.btnupd.Name = "btnupd";
            this.btnupd.Size = new System.Drawing.Size(70, 23);
            this.btnupd.TabIndex = 5;
            this.btnupd.Text = "刷新";
            this.btnupd.UseVisualStyleBackColor = true;
            this.btnupd.Click += new System.EventHandler(this.btnupd_Click);
            // 
            // chkupd
            // 
            this.chkupd.AutoSize = true;
            this.chkupd.Location = new System.Drawing.Point(519, 7);
            this.chkupd.Name = "chkupd";
            this.chkupd.Size = new System.Drawing.Size(75, 21);
            this.chkupd.TabIndex = 4;
            this.chkupd.Text = "自动刷新";
            this.chkupd.UseVisualStyleBackColor = true;
            // 
            // btngo
            // 
            this.btngo.Location = new System.Drawing.Point(162, 5);
            this.btngo.Name = "btngo";
            this.btngo.Size = new System.Drawing.Size(75, 23);
            this.btngo.TabIndex = 3;
            this.btngo.Text = "前往地址";
            this.btngo.UseVisualStyleBackColor = true;
            this.btngo.Click += new System.EventHandler(this.btngo_Click);
            // 
            // tbgoto
            // 
            this.tbgoto.Location = new System.Drawing.Point(32, 5);
            this.tbgoto.Name = "tbgoto";
            this.tbgoto.Size = new System.Drawing.Size(119, 23);
            this.tbgoto.TabIndex = 2;
            this.tbgoto.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbgoto_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "0x";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft YaHei UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label2.Location = new System.Drawing.Point(8, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 25);
            this.label2.TabIndex = 43;
            this.label2.Text = "内存搜索：";
            // 
            // btns
            // 
            this.btns.Location = new System.Drawing.Point(212, 44);
            this.btns.Name = "btns";
            this.btns.Size = new System.Drawing.Size(75, 26);
            this.btns.TabIndex = 42;
            this.btns.Text = "搜索";
            this.btns.UseVisualStyleBackColor = true;
            this.btns.Click += new System.EventHandler(this.btns_Click);
            // 
            // btnr
            // 
            this.btnr.Location = new System.Drawing.Point(212, 12);
            this.btnr.Name = "btnr";
            this.btnr.Size = new System.Drawing.Size(75, 23);
            this.btnr.TabIndex = 41;
            this.btnr.Text = "重置";
            this.btnr.UseVisualStyleBackColor = true;
            this.btnr.Click += new System.EventHandler(this.btnr_Click);
            // 
            // findb
            // 
            this.findb.Location = new System.Drawing.Point(13, 45);
            this.findb.Name = "findb";
            this.findb.Size = new System.Drawing.Size(186, 23);
            this.findb.TabIndex = 40;
            this.findb.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.findb_KeyPress);
            // 
            // gbst
            // 
            this.gbst.Controls.Add(this.rbfloat);
            this.gbst.Controls.Add(this.rbDword);
            this.gbst.Controls.Add(this.rbWord);
            this.gbst.Controls.Add(this.rbbyte);
            this.gbst.Location = new System.Drawing.Point(8, 80);
            this.gbst.Name = "gbst";
            this.gbst.Size = new System.Drawing.Size(289, 100);
            this.gbst.TabIndex = 39;
            this.gbst.TabStop = false;
            this.gbst.Text = "搜索类型";
            // 
            // rbfloat
            // 
            this.rbfloat.AutoSize = true;
            this.rbfloat.Location = new System.Drawing.Point(161, 59);
            this.rbfloat.Name = "rbfloat";
            this.rbfloat.Size = new System.Drawing.Size(86, 21);
            this.rbfloat.TabIndex = 42;
            this.rbfloat.TabStop = true;
            this.rbfloat.Text = "浮点(Float)";
            this.rbfloat.UseVisualStyleBackColor = true;
            // 
            // rbDword
            // 
            this.rbDword.AutoSize = true;
            this.rbDword.Location = new System.Drawing.Point(37, 59);
            this.rbDword.Name = "rbDword";
            this.rbDword.Size = new System.Drawing.Size(106, 21);
            this.rbDword.TabIndex = 41;
            this.rbDword.TabStop = true;
            this.rbDword.Text = "双字(DWORD)";
            this.rbDword.UseVisualStyleBackColor = true;
            // 
            // rbWord
            // 
            this.rbWord.AutoSize = true;
            this.rbWord.Location = new System.Drawing.Point(161, 32);
            this.rbWord.Name = "rbWord";
            this.rbWord.Size = new System.Drawing.Size(85, 21);
            this.rbWord.TabIndex = 40;
            this.rbWord.Text = "字(WORD)";
            this.rbWord.UseVisualStyleBackColor = true;
            // 
            // rbbyte
            // 
            this.rbbyte.AutoSize = true;
            this.rbbyte.Checked = true;
            this.rbbyte.Location = new System.Drawing.Point(36, 32);
            this.rbbyte.Name = "rbbyte";
            this.rbbyte.Size = new System.Drawing.Size(83, 21);
            this.rbbyte.TabIndex = 39;
            this.rbbyte.TabStop = true;
            this.rbbyte.Text = "字节(Byte)";
            this.rbbyte.UseVisualStyleBackColor = true;
            // 
            // labse
            // 
            this.labse.AutoSize = true;
            this.labse.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labse.Location = new System.Drawing.Point(7, 191);
            this.labse.Name = "labse";
            this.labse.Size = new System.Drawing.Size(225, 19);
            this.labse.TabIndex = 34;
            this.labse.Text = "搜索到 0 个地址  (只显示前500个)";
            // 
            // Form_Mem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(987, 532);
            this.Controls.Add(this.splitContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "Form_Mem";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "内存编辑";
            ((System.ComponentModel.ISupportInitialize)(this.ml)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.gbst.ResumeLayout(false);
            this.gbst.PerformLayout();
            this.ResumeLayout(false);

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
