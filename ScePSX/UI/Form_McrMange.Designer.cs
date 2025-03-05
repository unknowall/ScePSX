namespace ScePSX.UI
{
    partial class Form_McrMange
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
            cbsave1 = new System.Windows.Forms.ComboBox();
            cbsave2 = new System.Windows.Forms.ComboBox();
            lv1 = new System.Windows.Forms.ListView();
            GameIcon = new System.Windows.Forms.ColumnHeader();
            ID = new System.Windows.Forms.ColumnHeader();
            GameName = new System.Windows.Forms.ColumnHeader();
            move1to2 = new System.Windows.Forms.Button();
            move2to1 = new System.Windows.Forms.Button();
            del1 = new System.Windows.Forms.Button();
            out1 = new System.Windows.Forms.Button();
            save1 = new System.Windows.Forms.Button();
            save2 = new System.Windows.Forms.Button();
            out2 = new System.Windows.Forms.Button();
            del2 = new System.Windows.Forms.Button();
            lv2 = new System.Windows.Forms.ListView();
            columnHeader1 = new System.Windows.Forms.ColumnHeader();
            columnHeader2 = new System.Windows.Forms.ColumnHeader();
            columnHeader3 = new System.Windows.Forms.ColumnHeader();
            copy2to1 = new System.Windows.Forms.Button();
            copy1to2 = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // cbsave1
            // 
            cbsave1.FormattingEnabled = true;
            cbsave1.Location = new System.Drawing.Point(25, 12);
            cbsave1.Name = "cbsave1";
            cbsave1.Size = new System.Drawing.Size(344, 25);
            cbsave1.TabIndex = 0;
            // 
            // cbsave2
            // 
            cbsave2.FormattingEnabled = true;
            cbsave2.Location = new System.Drawing.Point(424, 12);
            cbsave2.Name = "cbsave2";
            cbsave2.Size = new System.Drawing.Size(344, 25);
            cbsave2.TabIndex = 1;
            // 
            // lv1
            // 
            lv1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { GameIcon, ID, GameName });
            lv1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            lv1.FullRowSelect = true;
            lv1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            lv1.LabelWrap = false;
            lv1.Location = new System.Drawing.Point(25, 43);
            lv1.MultiSelect = false;
            lv1.Name = "lv1";
            lv1.Size = new System.Drawing.Size(344, 374);
            lv1.TabIndex = 2;
            lv1.UseCompatibleStateImageBehavior = false;
            lv1.View = System.Windows.Forms.View.Details;
            // 
            // GameIcon
            // 
            GameIcon.Text = "";
            GameIcon.Width = 32;
            // 
            // ID
            // 
            ID.Text = "ID";
            ID.Width = 80;
            // 
            // GameName
            // 
            GameName.Text = "Name";
            GameName.Width = 120;
            // 
            // move1to2
            // 
            move1to2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            move1to2.Location = new System.Drawing.Point(378, 109);
            move1to2.Name = "move1to2";
            move1to2.Size = new System.Drawing.Size(37, 32);
            move1to2.TabIndex = 4;
            move1to2.Text = ">>";
            move1to2.UseVisualStyleBackColor = true;
            move1to2.Click += move1to2_Click;
            // 
            // move2to1
            // 
            move2to1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            move2to1.Location = new System.Drawing.Point(378, 165);
            move2to1.Name = "move2to1";
            move2to1.Size = new System.Drawing.Size(37, 32);
            move2to1.TabIndex = 5;
            move2to1.Text = "<<";
            move2to1.UseVisualStyleBackColor = true;
            move2to1.Click += move2to1_Click;
            // 
            // del1
            // 
            del1.Location = new System.Drawing.Point(23, 423);
            del1.Name = "del1";
            del1.Size = new System.Drawing.Size(73, 32);
            del1.TabIndex = 6;
            del1.Text = "删除选中";
            del1.UseVisualStyleBackColor = true;
            del1.Click += del1_Click;
            // 
            // out1
            // 
            out1.Location = new System.Drawing.Point(105, 423);
            out1.Name = "out1";
            out1.Size = new System.Drawing.Size(73, 32);
            out1.TabIndex = 8;
            out1.Text = "导出";
            out1.UseVisualStyleBackColor = true;
            out1.Click += out1_Click;
            // 
            // save1
            // 
            save1.Location = new System.Drawing.Point(187, 423);
            save1.Name = "save1";
            save1.Size = new System.Drawing.Size(73, 32);
            save1.TabIndex = 9;
            save1.Text = "保存修改";
            save1.UseVisualStyleBackColor = true;
            save1.Click += save1_Click;
            // 
            // save2
            // 
            save2.Location = new System.Drawing.Point(695, 423);
            save2.Name = "save2";
            save2.Size = new System.Drawing.Size(73, 32);
            save2.TabIndex = 12;
            save2.Text = "保存修改";
            save2.UseVisualStyleBackColor = true;
            save2.Click += save2_Click;
            // 
            // out2
            // 
            out2.Location = new System.Drawing.Point(613, 423);
            out2.Name = "out2";
            out2.Size = new System.Drawing.Size(73, 32);
            out2.TabIndex = 11;
            out2.Text = "导出";
            out2.UseVisualStyleBackColor = true;
            out2.Click += out2_Click;
            // 
            // del2
            // 
            del2.Location = new System.Drawing.Point(531, 423);
            del2.Name = "del2";
            del2.Size = new System.Drawing.Size(73, 32);
            del2.TabIndex = 10;
            del2.Text = "删除选中";
            del2.UseVisualStyleBackColor = true;
            del2.Click += del2_Click;
            // 
            // lv2
            // 
            lv2.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3 });
            lv2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            lv2.FullRowSelect = true;
            lv2.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            lv2.LabelWrap = false;
            lv2.Location = new System.Drawing.Point(424, 43);
            lv2.MultiSelect = false;
            lv2.Name = "lv2";
            lv2.Size = new System.Drawing.Size(344, 374);
            lv2.TabIndex = 13;
            lv2.UseCompatibleStateImageBehavior = false;
            lv2.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "";
            columnHeader1.Width = 32;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "ID";
            columnHeader2.Width = 80;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Name";
            columnHeader3.Width = 120;
            // 
            // copy2to1
            // 
            copy2to1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            copy2to1.Location = new System.Drawing.Point(378, 275);
            copy2to1.Name = "copy2to1";
            copy2to1.Size = new System.Drawing.Size(37, 32);
            copy2to1.TabIndex = 15;
            copy2to1.Text = "<";
            copy2to1.UseVisualStyleBackColor = true;
            copy2to1.Click += copy2to1_Click;
            // 
            // copy1to2
            // 
            copy1to2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            copy1to2.Location = new System.Drawing.Point(378, 219);
            copy1to2.Name = "copy1to2";
            copy1to2.Size = new System.Drawing.Size(37, 32);
            copy1to2.TabIndex = 14;
            copy1to2.Text = ">";
            copy1to2.UseVisualStyleBackColor = true;
            copy1to2.Click += copy1to2_Click;
            // 
            // Form_McrMange
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(790, 467);
            Controls.Add(copy2to1);
            Controls.Add(copy1to2);
            Controls.Add(lv2);
            Controls.Add(save2);
            Controls.Add(out2);
            Controls.Add(del2);
            Controls.Add(save1);
            Controls.Add(out1);
            Controls.Add(del1);
            Controls.Add(move2to1);
            Controls.Add(move1to2);
            Controls.Add(lv1);
            Controls.Add(cbsave2);
            Controls.Add(cbsave1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form_McrMange";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "存档管理";
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ComboBox cbsave1;
        private System.Windows.Forms.ComboBox cbsave2;
        private System.Windows.Forms.ListView lv1;
        private System.Windows.Forms.Button move1to2;
        private System.Windows.Forms.Button move2to1;
        private System.Windows.Forms.Button del1;
        private System.Windows.Forms.Button out1;
        private System.Windows.Forms.Button save1;
        private System.Windows.Forms.Button save2;
        private System.Windows.Forms.Button out2;
        private System.Windows.Forms.Button del2;
        private System.Windows.Forms.ColumnHeader GameIcon;
        private System.Windows.Forms.ColumnHeader GameName;
        private System.Windows.Forms.ColumnHeader ID;
        private System.Windows.Forms.ListView lv2;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.Button copy2to1;
        private System.Windows.Forms.Button copy1to2;
    }
}
