namespace ScePSX.UI
{
    partial class FrmAbout
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
            labver = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            textBox1 = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            linkLabel1 = new System.Windows.Forms.LinkLabel();
            SuspendLayout();
            // 
            // labver
            // 
            labver.AutoSize = true;
            labver.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            labver.Location = new System.Drawing.Point(26, 21);
            labver.Name = "labver";
            labver.Size = new System.Drawing.Size(150, 22);
            labver.TabIndex = 0;
            labver.Text = "ScePSX Beta 0.05";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(26, 141);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(70, 17);
            label2.TabIndex = 1;
            label2.Text = "Maintainer";
            // 
            // textBox1
            // 
            textBox1.Location = new System.Drawing.Point(34, 161);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.Size = new System.Drawing.Size(254, 58);
            textBox1.TabIndex = 2;
            textBox1.Text = "unknowall - sgfree@hotmail.com";
            // 
            // label3
            // 
            label3.Location = new System.Drawing.Point(34, 53);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(350, 56);
            label3.TabIndex = 3;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new System.Drawing.Point(35, 111);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new System.Drawing.Size(225, 17);
            linkLabel1.TabIndex = 4;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "https://github.com/unknowall/ScePSX";
            linkLabel1.LinkClicked += Link_LinkClicked;
            // 
            // FrmAbout
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(318, 239);
            Controls.Add(linkLabel1);
            Controls.Add(label3);
            Controls.Add(textBox1);
            Controls.Add(label2);
            Controls.Add(labver);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FrmAbout";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "About";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label labver;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.LinkLabel linkLabel1;
    }
}
