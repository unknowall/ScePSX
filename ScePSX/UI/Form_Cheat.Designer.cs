namespace ScePSX.UI
{
    partial class Form_Cheat
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
            btnadd = new System.Windows.Forms.Button();
            btndel = new System.Windows.Forms.Button();
            ctb = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            btnload = new System.Windows.Forms.Button();
            btnimp = new System.Windows.Forms.Button();
            btnsave = new System.Windows.Forms.Button();
            btnapply = new System.Windows.Forms.Button();
            clb = new System.Windows.Forms.ListView();
            columnHeader1 = new System.Windows.Forms.ColumnHeader();
            BtnSearch = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // btnadd
            // 
            btnadd.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold);
            btnadd.Location = new System.Drawing.Point(12, 10);
            btnadd.Name = "btnadd";
            btnadd.Size = new System.Drawing.Size(57, 26);
            btnadd.TabIndex = 0;
            btnadd.Text = Properties.Resources.Form_Cheat_InitializeComponent_增加;
            btnadd.UseVisualStyleBackColor = true;
            btnadd.Click += btnadd_Click;
            // 
            // btndel
            // 
            btndel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold);
            btndel.Location = new System.Drawing.Point(75, 10);
            btndel.Name = "btndel";
            btndel.Size = new System.Drawing.Size(55, 26);
            btndel.TabIndex = 1;
            btndel.Text = Properties.Resources.Form_Cheat_InitializeComponent_删除;
            btndel.UseVisualStyleBackColor = true;
            btndel.Click += btndel_Click;
            // 
            // ctb
            // 
            ctb.Font = new System.Drawing.Font("Tahoma", 12F);
            ctb.Location = new System.Drawing.Point(250, 41);
            ctb.Multiline = true;
            ctb.Name = "ctb";
            ctb.Size = new System.Drawing.Size(227, 310);
            ctb.TabIndex = 3;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold);
            label1.Location = new System.Drawing.Point(250, 18);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(51, 19);
            label1.TabIndex = 4;
            label1.Text = "Codes";
            // 
            // btnload
            // 
            btnload.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold);
            btnload.Location = new System.Drawing.Point(22, 361);
            btnload.Name = "btnload";
            btnload.Size = new System.Drawing.Size(65, 26);
            btnload.TabIndex = 5;
            btnload.Text = Properties.Resources.Form_Cheat_InitializeComponent_读取;
            btnload.UseVisualStyleBackColor = true;
            btnload.Click += btnload_Click;
            // 
            // btnimp
            // 
            btnimp.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold);
            btnimp.Location = new System.Drawing.Point(162, 361);
            btnimp.Name = "btnimp";
            btnimp.Size = new System.Drawing.Size(64, 26);
            btnimp.TabIndex = 6;
            btnimp.Text = Properties.Resources.Form_Cheat_InitializeComponent_导入;
            btnimp.UseVisualStyleBackColor = true;
            btnimp.Click += btnimp_Click;
            // 
            // btnsave
            // 
            btnsave.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold);
            btnsave.Location = new System.Drawing.Point(93, 361);
            btnsave.Name = "btnsave";
            btnsave.Size = new System.Drawing.Size(63, 26);
            btnsave.TabIndex = 7;
            btnsave.Text = Properties.Resources.Form_Cheat_InitializeComponent_保存;
            btnsave.UseVisualStyleBackColor = true;
            btnsave.Click += btnsave_Click;
            // 
            // btnapply
            // 
            btnapply.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold);
            btnapply.Location = new System.Drawing.Point(356, 361);
            btnapply.Name = "btnapply";
            btnapply.Size = new System.Drawing.Size(121, 26);
            btnapply.TabIndex = 8;
            btnapply.Text = Properties.Resources.Form_Cheat_InitializeComponent_应用金手指;
            btnapply.UseVisualStyleBackColor = true;
            btnapply.Click += btnapply_Click;
            // 
            // clb
            // 
            clb.CheckBoxes = true;
            clb.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { columnHeader1 });
            clb.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            clb.FullRowSelect = true;
            clb.LabelEdit = true;
            clb.LabelWrap = false;
            clb.Location = new System.Drawing.Point(12, 42);
            clb.MultiSelect = false;
            clb.Name = "clb";
            clb.Size = new System.Drawing.Size(223, 309);
            clb.TabIndex = 9;
            clb.UseCompatibleStateImageBehavior = false;
            clb.View = System.Windows.Forms.View.Details;
            clb.SelectedIndexChanged += clb_SelectedIndexChanged;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Cheat List";
            columnHeader1.Width = 300;
            // 
            // BtnSearch
            // 
            BtnSearch.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            BtnSearch.Location = new System.Drawing.Point(318, 9);
            BtnSearch.Name = "BtnSearch";
            BtnSearch.Size = new System.Drawing.Size(159, 26);
            BtnSearch.TabIndex = 10;
            BtnSearch.Text = Properties.Resources.Form_Cheat_InitializeComponent_FetchCheatsOnline;
            BtnSearch.UseVisualStyleBackColor = true;
            BtnSearch.Click += BtnSearch_Click;
            // 
            // Form_Cheat
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(489, 400);
            Controls.Add(BtnSearch);
            Controls.Add(clb);
            Controls.Add(btnapply);
            Controls.Add(btnsave);
            Controls.Add(btnimp);
            Controls.Add(btnload);
            Controls.Add(label1);
            Controls.Add(ctb);
            Controls.Add(btndel);
            Controls.Add(btnadd);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form_Cheat";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Cheat Codes";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnadd;
        private System.Windows.Forms.Button btndel;
        private System.Windows.Forms.TextBox ctb;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnload;
        private System.Windows.Forms.Button btnimp;
        private System.Windows.Forms.Button btnsave;
        private System.Windows.Forms.Button btnapply;
        private System.Windows.Forms.ListView clb;
        private System.Windows.Forms.Button BtnSearch;
        private System.Windows.Forms.ColumnHeader columnHeader1;
    }
}
