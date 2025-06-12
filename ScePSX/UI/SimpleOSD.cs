using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScePSX.UI
{
    public class SimpleOSD : Control
    {
        private Timer hideTimer;
        private int displayTime = 3000;
        private string currentMessage = string.Empty;
        private Color _BgColor;
        public static Control _parent;

        public static void Show(Control parent, string message, int displayTime = 3000, Color BgColor = default)
        {
            _parent = parent;

            foreach (Control control in _parent.Controls)
            {
                if (control is SimpleOSD existingOsd)
                {
                    existingOsd.ShowMessage(message, displayTime, BgColor);
                    return;
                }
            }
            SimpleOSD osd = new SimpleOSD();
            parent.Controls.Add(osd);
            osd.BringToFront();
            osd.ShowMessage(message, displayTime, BgColor);
        }

        public static void Close()
        {
            if (_parent == null || _parent.Controls.Count == 0)
                return;

            foreach (Control control in _parent.Controls)
            {
                if (control != null && control is SimpleOSD existingOsd)
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
            this.Size = new Size(40, 40);
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

        public void ShowMessage(string message, int displayTime = 3000, Color BgColor = default)
        {
            this._BgColor = BgColor == default ? Color.FromArgb(200, 35, 35, 35) : BgColor;
            this.displayTime = displayTime;
            currentMessage = message;
            if (!string.IsNullOrEmpty(message))
            {
                using (var g = this.CreateGraphics())
                using (var font = new Font("Arial", 10, FontStyle.Bold))
                {
                    var format = new StringFormat(StringFormatFlags.NoWrap);
                    var textSize = g.MeasureString(message, font, int.MaxValue, format);
                    int newWidth = Math.Max(40, (int)textSize.Width + 30);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            using (Brush backBrush = new SolidBrush(_BgColor))
            {
                g.FillRectangle(backBrush, this.ClientRectangle);
            }

            using (Pen borderPen = new Pen(Color.FromArgb(90, 90, 90), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            if (!string.IsNullOrEmpty(currentMessage))
            {
                TextRenderer.DrawText(
                    g,
                    currentMessage,
                    new Font("Arial", 10, FontStyle.Bold),
                    new Rectangle(6, 4, Width - 12, Height - 8),
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );
            }
        }
    }
}
