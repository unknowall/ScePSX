using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ScePSX.UI
{
    public class BaseDialogForm : Form
    {
        protected Color _titleBarColor = Color.FromArgb(45, 45, 45);
        protected Color _borderColor = Color.FromArgb(100, 100, 100);
        protected Color _contentBackColor = Color.FromArgb(45, 45, 45);
        protected Font _titleFont = new Font("Arial", 10, FontStyle.Bold);
        protected int _titleBarHeight = 30;

        public Label titleLabel;
        public Panel titleBar;

        public BaseDialogForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = _titleBarColor;
            ForeColor = Color.White;
            Padding = new Padding(2, 2, 2, 2);
            DoubleBuffered = true;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new FormBorderStyle FormBorderStyle
        {
            get => base.FormBorderStyle;
            set
            {
                base.FormBorderStyle = FormBorderStyle.None;
            }
        }

        public void AddTitleButton(string text, Action action, int x, Color entercolor)
        {
            Label Button = new Label
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Franklin Gothic", 14f, FontStyle.Bold),
                AutoSize = false,
                Width = 35,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            Button.Location = new Point(this.Width - x, 0);
            Button.MouseEnter += (sender, e) => Button.BackColor = entercolor;
            Button.MouseLeave += (sender, e) => Button.BackColor = Color.Transparent;
            Button.Click += (s, e) => action();
            titleBar.Controls.Add(Button);
        }

        private void ToggleMaximize()
        {
            WindowState = (WindowState == FormWindowState.Maximized)
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }

        bool isDragging = false;
        Point dragStart = new Point();

        private void BarMouseDown(MouseEventArgs e)
        {
            isDragging = true;
            dragStart = new Point(e.X, e.Y);
        }

        private void BarMouseMove(MouseEventArgs e)
        {
            if (isDragging)
            {
                Point p = PointToScreen(e.Location);
                this.Location = new Point(p.X - dragStart.X, p.Y - dragStart.Y);
            }
        }

        private void BarMouseUp(MouseEventArgs e)
        {
            isDragging = false;
        }

        public void AddCustomTitleBar()
        {
            titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(48, 48, 48)
            };
            this.Controls.Add(titleBar);

            // Title label
            titleLabel = new Label
            {
                Text = this.Text,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Franklin Gothic", 10f, FontStyle.Regular),
                AutoSize = true,
                Width = 150,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft
            };
            titleLabel.Location = new Point(6, 0);
            titleBar.Controls.Add(titleLabel);

            AddTitleButton("×", Close, 35, Color.FromArgb(232, 17, 35));
            //AddTitleButton("□", ToggleMaximize, 70, Color.FromArgb(209, 209, 209));
            AddTitleButton("−", () => WindowState = FormWindowState.Minimized, 70, Color.FromArgb(209, 209, 209));

            titleLabel.MouseDown += (sender, e) => BarMouseDown(e);

            titleBar.MouseDown += (sender, e) => BarMouseDown(e);

            titleLabel.MouseMove += (sender, e) => BarMouseMove(e);

            titleBar.MouseMove += (sender, e) => BarMouseMove(e);

            titleLabel.MouseUp += (sender, e) => BarMouseUp(e);

            titleBar.MouseUp += (sender, e) => BarMouseUp(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ApplyGlobalStyles();

            AddCustomTitleBar();
        }

        private void ApplyGlobalStyles()
        {
            foreach (Control control in Controls)
            {
                // 统一背景色
                control.BackColor = _contentBackColor;
                // 统一字体颜色
                if (control is Label || control is LinkLabel)
                {
                    control.ForeColor = Color.White;
                }
                else if (control is TextBox)
                {
                    control.ForeColor = Color.LightGray;
                }
            }
        }
    }
}
