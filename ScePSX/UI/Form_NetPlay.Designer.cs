﻿namespace ScePSX.UI
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
            tblocalip = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            tbsrvip = new System.Windows.Forms.TextBox();
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
            btncli.Text = ScePSX.Properties.Resources.FrmNetPlay_InitializeComponent_作为客户机启动;
            btncli.UseVisualStyleBackColor = true;
            btncli.Click += btncli_Click;
            // 
            // tblocalip
            // 
            tblocalip.Location = new System.Drawing.Point(25, 49);
            tblocalip.Name = "tblocalip";
            tblocalip.Size = new System.Drawing.Size(233, 23);
            tblocalip.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(25, 22);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(127, 17);
            label1.TabIndex = 2;
            label1.Text = ScePSX.Properties.Resources.FrmNetPlay_InitializeComponent_作为主机时的IP地址;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(25, 95);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(115, 17);
            label2.TabIndex = 3;
            label2.Text = ScePSX.Properties.Resources.FrmNetPlay_InitializeComponent_目标主机的IP地址;
            // 
            // tbsrvip
            // 
            tbsrvip.Location = new System.Drawing.Point(25, 121);
            tbsrvip.Name = "tbsrvip";
            tbsrvip.Size = new System.Drawing.Size(233, 23);
            tbsrvip.TabIndex = 4;
            // 
            // btnsrv
            // 
            btnsrv.Location = new System.Drawing.Point(25, 163);
            btnsrv.Name = "btnsrv";
            btnsrv.Size = new System.Drawing.Size(104, 29);
            btnsrv.TabIndex = 5;
            btnsrv.Text = ScePSX.Properties.Resources.FrmNetPlay_InitializeComponent_作为主机启动;
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
            labhint.Text = ScePSX.Properties.Resources.FrmNetPlay_InitializeComponent_未启动网络对战;
            // 
            // FrmNetPlay
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(280, 225);
            Controls.Add(labhint);
            Controls.Add(btnsrv);
            Controls.Add(tbsrvip);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(tblocalip);
            Controls.Add(btncli);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FrmNetPlay";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "NetPlay";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btncli;
        private System.Windows.Forms.TextBox tblocalip;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbsrvip;
        private System.Windows.Forms.Button btnsrv;
        private System.Windows.Forms.Label labhint;
    }
}
