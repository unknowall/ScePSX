using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ScePSX.UI
{
    public class SimpleOSD : Control
    {
        private Timer hideTimer;
        private int displayTime = 3000;
        private string currentMessage = string.Empty;

        public static void Show(Control parent, string message, int displayTime = 3000)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is SimpleOSD existingOsd)
                {
                    existingOsd.ShowMessage(message, displayTime);
                    return;
                }
            }

            SimpleOSD osd = new SimpleOSD();
            parent.Controls.Add(osd);
            osd.BringToFront();
            osd.ShowMessage(message, displayTime);
        }

        public static void Hide(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is SimpleOSD existingOsd)
                {
                    existingOsd.hideTimer.Stop();
                    existingOsd.Hide();
                    return;
                }
            }
        }

        public SimpleOSD()
        {
            this.DoubleBuffered = true;
            this.Size = new Size(200, 40);
            //this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            //this.BackColor = Color.Transparent;
            this.BackColor = Color.FromArgb(255, 0, 0, 180);

            hideTimer = new Timer();
            hideTimer.Interval = displayTime;
            hideTimer.Tick += HideTimer_Tick;

            this.Visible = false;
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            hideTimer.Stop();
            this.Hide();
        }

        public void ShowMessage(string message, int displayTime = 3000)
        {
            this.displayTime = displayTime;
            currentMessage = message;

            if (!string.IsNullOrEmpty(message))
            {
                using (var g = this.CreateGraphics())
                using (var font = new Font("Arial", 14, FontStyle.Bold))
                {
                    var format = new StringFormat(StringFormatFlags.NoWrap);
                    var textSize = g.MeasureString(message, font, int.MaxValue, format);
                    int newWidth = Math.Max(200, (int)textSize.Width + 40);

                    this.Size = new Size(newWidth, 40);
                }
            }

            if (this.Parent != null)
            {
                this.Location = new Point(10, 10);
            }

            this.Show();
            this.BringToFront();

            hideTimer.Stop();
            hideTimer.Interval = displayTime;
            hideTimer.Start();

            this.Invalidate();
        }

        //protected override void OnPaintBackground(PaintEventArgs e)
        //{
        //    using (var brush = new SolidBrush(Color.FromArgb(180, 0, 0, 180)))
        //    {
        //        e.Graphics.FillRectangle(brush, this.ClientRectangle);
        //    }
        //}

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var pen = new Pen(Color.Yellow, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }

            if (!string.IsNullOrEmpty(currentMessage))
            {
                using (var brush = new SolidBrush(Color.Yellow))
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;

                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                    e.Graphics.DrawString(
                        currentMessage,
                        new Font("Arial", 14, FontStyle.Bold),
                        brush,
                        this.ClientRectangle,
                        format
                    );
                }
            }
        }
    }
}
