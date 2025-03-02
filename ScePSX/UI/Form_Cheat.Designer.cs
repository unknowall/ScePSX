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
            this.btnadd = new System.Windows.Forms.Button();
            this.btndel = new System.Windows.Forms.Button();
            this.ctb = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnload = new System.Windows.Forms.Button();
            this.btnimp = new System.Windows.Forms.Button();
            this.btnsave = new System.Windows.Forms.Button();
            this.btnapply = new System.Windows.Forms.Button();
            this.clb = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // btnadd
            // 
            this.btnadd.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnadd.Location = new System.Drawing.Point(12, 10);
            this.btnadd.Name = "btnadd";
            this.btnadd.Size = new System.Drawing.Size(57, 26);
            this.btnadd.TabIndex = 0;
            this.btnadd.Text = "增加";
            this.btnadd.UseVisualStyleBackColor = true;
            this.btnadd.Click += new System.EventHandler(this.btnadd_Click);
            // 
            // btndel
            // 
            this.btndel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btndel.Location = new System.Drawing.Point(75, 10);
            this.btndel.Name = "btndel";
            this.btndel.Size = new System.Drawing.Size(55, 26);
            this.btndel.TabIndex = 1;
            this.btndel.Text = "删除";
            this.btndel.UseVisualStyleBackColor = true;
            this.btndel.Click += new System.EventHandler(this.btndel_Click);
            // 
            // ctb
            // 
            this.ctb.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ctb.Location = new System.Drawing.Point(250, 41);
            this.ctb.Multiline = true;
            this.ctb.Name = "ctb";
            this.ctb.Size = new System.Drawing.Size(227, 310);
            this.ctb.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(250, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 19);
            this.label1.TabIndex = 4;
            this.label1.Text = "地址代码:";
            // 
            // btnload
            // 
            this.btnload.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnload.Location = new System.Drawing.Point(22, 361);
            this.btnload.Name = "btnload";
            this.btnload.Size = new System.Drawing.Size(65, 26);
            this.btnload.TabIndex = 5;
            this.btnload.Text = "读取";
            this.btnload.UseVisualStyleBackColor = true;
            this.btnload.Click += new System.EventHandler(this.btnload_Click);
            // 
            // btnimp
            // 
            this.btnimp.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnimp.Location = new System.Drawing.Point(162, 361);
            this.btnimp.Name = "btnimp";
            this.btnimp.Size = new System.Drawing.Size(64, 26);
            this.btnimp.TabIndex = 6;
            this.btnimp.Text = "导入";
            this.btnimp.UseVisualStyleBackColor = true;
            this.btnimp.Click += new System.EventHandler(this.btnimp_Click);
            // 
            // btnsave
            // 
            this.btnsave.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnsave.Location = new System.Drawing.Point(93, 361);
            this.btnsave.Name = "btnsave";
            this.btnsave.Size = new System.Drawing.Size(63, 26);
            this.btnsave.TabIndex = 7;
            this.btnsave.Text = "保存";
            this.btnsave.UseVisualStyleBackColor = true;
            this.btnsave.Click += new System.EventHandler(this.btnsave_Click);
            // 
            // btnapply
            // 
            this.btnapply.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnapply.Location = new System.Drawing.Point(356, 361);
            this.btnapply.Name = "btnapply";
            this.btnapply.Size = new System.Drawing.Size(121, 26);
            this.btnapply.TabIndex = 8;
            this.btnapply.Text = "应用金手指";
            this.btnapply.UseVisualStyleBackColor = true;
            this.btnapply.Click += new System.EventHandler(this.btnapply_Click);
            // 
            // clb
            // 
            this.clb.CheckBoxes = true;
            this.clb.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.clb.FullRowSelect = true;
            this.clb.LabelEdit = true;
            this.clb.Location = new System.Drawing.Point(12, 42);
            this.clb.MultiSelect = false;
            this.clb.Name = "clb";
            this.clb.Size = new System.Drawing.Size(223, 309);
            this.clb.TabIndex = 9;
            this.clb.UseCompatibleStateImageBehavior = false;
            this.clb.View = System.Windows.Forms.View.List;
            this.clb.SelectedIndexChanged += new System.EventHandler(this.clb_SelectedIndexChanged);
            // 
            // Form_Cheat
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(489, 400);
            this.Controls.Add(this.clb);
            this.Controls.Add(this.btnapply);
            this.Controls.Add(this.btnsave);
            this.Controls.Add(this.btnimp);
            this.Controls.Add(this.btnload);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ctb);
            this.Controls.Add(this.btndel);
            this.Controls.Add(this.btnadd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form_Cheat";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "金手指";
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}
