namespace ScePSX.UI
{
    partial class FrmNetPlay
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
            btncli = new System.Windows.Forms.Button();
            textBox1 = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            textBox2 = new System.Windows.Forms.TextBox();
            btnsrv = new System.Windows.Forms.Button();
            labhint = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // btncli
            // 
            btncli.Location = new System.Drawing.Point(157, 163);
            btncli.Name = "btncli";
            btncli.Size = new System.Drawing.Size(101, 29);
            btncli.TabIndex = 0;
            btncli.Text = "作为客户机启动";
            btncli.UseVisualStyleBackColor = true;
            btncli.Click += btncli_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new System.Drawing.Point(25, 49);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(233, 23);
            textBox1.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(25, 22);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(127, 17);
            label1.TabIndex = 2;
            label1.Text = "作为主机时的IP地址：";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(25, 95);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(115, 17);
            label2.TabIndex = 3;
            label2.Text = "目标主机的IP地址：";
            // 
            // textBox2
            // 
            textBox2.Location = new System.Drawing.Point(25, 121);
            textBox2.Name = "textBox2";
            textBox2.Size = new System.Drawing.Size(233, 23);
            textBox2.TabIndex = 4;
            // 
            // btnsrv
            // 
            btnsrv.Location = new System.Drawing.Point(25, 163);
            btnsrv.Name = "btnsrv";
            btnsrv.Size = new System.Drawing.Size(104, 29);
            btnsrv.TabIndex = 5;
            btnsrv.Text = "作为主机启动";
            btnsrv.UseVisualStyleBackColor = true;
            btnsrv.Click += btnsrv_Click;
            // 
            // labhint
            // 
            labhint.AutoSize = true;
            labhint.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            labhint.Location = new System.Drawing.Point(25, 199);
            labhint.Name = "labhint";
            labhint.Size = new System.Drawing.Size(107, 19);
            labhint.TabIndex = 6;
            labhint.Text = "未启动网络对战";
            // 
            // FrmNetPlay
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(280, 225);
            Controls.Add(labhint);
            Controls.Add(btnsrv);
            Controls.Add(textBox2);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(btncli);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FrmNetPlay";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "网络对战";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btncli;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button btnsrv;
        private System.Windows.Forms.Label labhint;
    }
}
