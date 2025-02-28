using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace HexBoxControl
{
    public enum HexBoxViewMode
    {
        Bytes,
        Words,
        BytesAscii
    };

    public interface ICharConverter
    {
        char ToChar(byte[] dump, int offset);

        byte ToByte(char c);
    }

    public class AnsiCharConvertor : ICharConverter
    {
        public virtual char ToChar(byte[] dump, int offset)
        {
            char c = (char)dump[offset];
            return (c < '!') || ('\x7e' < c && c < '\xa1') || (c == '\xad') ? '\0' : c;
        }

        public virtual byte ToByte(char c)
        {
            return (byte)c;
        }

        public override string ToString()
        {
            return "ANSI";
        }
    }

    public class AsciiCharConvertor : ICharConverter
    {
        private Encoding _Encoding = Encoding.ASCII;

        public virtual char ToChar(byte[] dump, int offset)
        {
            char c = _Encoding.GetChars(dump, offset, 1)[0];
            return (c < '!') || (c == '\x7f') ? '\0' : c;
        }

        public virtual byte ToByte(char c)
        {
            byte[] decoded = _Encoding.GetBytes(new char[] { c });
            return decoded.Length > 0 ? decoded[0] : (byte)0;
        }

        public override string ToString()
        {
            return "ASCII";
        }
    }

    public class Utf8CharConvertor : ICharConverter
    {
        private Encoding _Encoding = Encoding.UTF8;

        public virtual char ToChar(byte[] dump, int offset)
        {
            char c = _Encoding.GetChars(dump, offset, 1)[0];
            return (c < '!') || (c == '\x7f') ? '\0' : c;
        }

        public virtual byte ToByte(char c)
        {
            byte[] decoded = _Encoding.GetBytes(new char[] { c });
            return decoded.Length > 0 ? decoded[0] : (byte)0;
        }

        public override string ToString()
        {
            return "UTF-8";
        }
    }

    public class HexBoxEditEventArgs : EventArgs
    {
        #region Fields
        private int _Offset;
        private int _OldValue;
        private int _NewValue;
        #endregion

        #region Ctors
        public HexBoxEditEventArgs(int offset, int prev, int current)
        {
            _Offset = offset;
            _OldValue = prev;
            _NewValue = current;
        }
        #endregion

        #region Properties
        public int Offset
        {
            get
            {
                return _Offset;
            }
        }

        public int OldValue
        {
            get
            {
                return _OldValue;
            }
        }

        public int NewValue
        {
            get
            {
                return _NewValue;
            }
        }
        #endregion
    }

    public delegate void HexBoxEditEventHandler(object sender, HexBoxEditEventArgs e);

    public class HexBox : Control
    {
        #region Fields
        private int _HeaderLeft = 4;
        private int _HeaderTop = 2;
        private int _ColumnsDelim = 10;
        private int _CharWidth;
        private int _CharHeight;
        private int _AddressWidth;
        private int _DataCellWidth;
        private int _DataColums = 8;
        private byte[] _Dump;
        private int _Lines;
        private int _DataRows;
        private long _Offset = 0;
        private bool _ColumnsAuto = false;
        private int _LineLength;
        private int _EditIndex = -1;
        private bool ResetViewByLoad = true;
        private long _AddressOffset = 0;

        private HexBoxViewMode _ViewMode;
        private ICharConverter _CharConverter;
        private StringFormat _StringFormat;
        private VScrollBar _ScrollBar;
        private TextBox _Edit;
        #endregion

        #region Ctors
        public HexBox()
        {
            _ViewMode = HexBoxViewMode.BytesAscii;
            _CharConverter = new AnsiCharConvertor();
            _StringFormat = new StringFormat(StringFormat.GenericTypographic);
            _StringFormat.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;

            ClientSize = new Size(380, 198);

            _ScrollBar = new VScrollBar();
            _ScrollBar.Size = new Size(16, this.Height - 2);
            _ScrollBar.Location = new Point(Width - (16 + 1), 1);
            _ScrollBar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            _ScrollBar.Scroll += OnScrolling;
            _ScrollBar.Enabled = false;
            Controls.Add(_ScrollBar);

            _Edit = new TextBox();
            _Edit.BorderStyle = BorderStyle.None;
            _Edit.Size = new Size(18, 18);
            _Edit.MaxLength = 2;
            _Edit.Font = Font;
            _Edit.ForeColor = Color.Green;
            _Edit.ContextMenuStrip = new ContextMenuStrip();
            _Edit.KeyPress += EditKeyPress;
            Controls.Add(_Edit);
            _Edit.Hide();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            Resume();
        }
        #endregion

        #region Properties
        public HexBoxViewMode ViewMode
        {
            get
            {
                return _ViewMode;
            }

            set
            {
                if (value != _ViewMode)
                {
                    EndEditing();

                    _ViewMode = value;

                    _Edit.Width = 18 * GetCellBytes();
                    _Edit.MaxLength = 2 * GetCellBytes();
                    _Offset = 0;

                    Resume();
                    ResumeScroll();
                    Invalidate();
                }
            }
        }

        public int Columns
        {
            get
            {
                return _DataColums;
            }

            set
            {
                if (value > 0 && value != _DataColums)
                {
                    _DataColums = value;

                    EndEditing();

                    Resume();
                    ResumeScroll();
                    Invalidate();
                }
            }
        }

        public long AddressOffset
        {
            get
            {
                return _AddressOffset;
            }
            set
            {
                _AddressOffset = value;
            }
        }

        public bool ResetOffset
        {
            get
            {
                return ResetViewByLoad;
            }
            set
            {
                ResetViewByLoad = value;
            }
        }

        public int Rows
        {
            get
            {
                return _Lines;
            }
        }

        public int DisplayedRows
        {
            get
            {
                return _DataRows;
            }
        }

        public byte[] Dump
        {
            get
            {
                return _Dump;
            }

            set
            {
                _Dump = value;

                if (ResetViewByLoad)
                    _Offset = 0;
                _EditIndex = -1;
                _Edit.Hide();

                Resume();
                ResumeScroll();
                Invalidate();
            }
        }

        public ICharConverter CharConverter
        {
            get
            {
                return _CharConverter;
            }

            set
            {
                if (value != null && value != _CharConverter)
                {
                    _CharConverter = value;

                    if (_ViewMode == HexBoxViewMode.BytesAscii)
                    {
                        Invalidate();
                    }
                }
            }
        }

        public bool ColumnsAuto
        {
            get
            {
                return _ColumnsAuto;
            }

            set
            {
                if (value != _ColumnsAuto)
                {
                    _ColumnsAuto = value;

                    EndEditing();

                    Resume();
                    ResumeScroll();
                    Invalidate();
                }
            }
        }

        public override Font Font
        {
            get
            {
                return base.Font;
            }

            set
            {
                if (value != base.Font)
                {
                    EndEditing();

                    base.Font = value;
                    _Edit.Font = value;

                    Resume();
                    ResumeScroll();
                    Invalidate();
                }

            }
        }

        public new bool Enabled
        {
            get
            {
                return base.Enabled;
            }

            set
            {
                if (value != base.Enabled)
                {
                    base.Enabled = value;
                    EndEditing();
                }
            }
        }
        #endregion

        #region Methods
        public void Fill(int size, int value)
        {
            byte[] dump = new byte[size * GetCellBytes()];

            if (_ViewMode == HexBoxViewMode.Words)
            {
                byte low = (byte)(value & 0xFF);
                byte high = (byte)(value >> 8);

                for (int i = 0; i < size; i++)
                {
                    dump[2 * i + 0] = low;
                    dump[2 * i + 1] = high;
                }
            } else
            {
                for (int i = 0; i < size; i++)
                {
                    dump[i] = (byte)value;
                }
            }

            Dump = dump;
        }

        public void ScrollTo(long pos)
        {
            if (pos - _AddressOffset > Dump.Length || pos < 0)
                return;

            _Offset = pos - _AddressOffset;

            Resume();
            ResumeScroll();
            if (_Offset > 0)
            {
                int dl = _Lines - _DataRows;

                if (dl < (_Offset / _LineLength))
                {
                    EndEditing();

                    _Offset = (dl * _LineLength);
                    _ScrollBar.Value = dl;
                }
            }
            Invalidate();
        }
        #endregion

        #region Events
        public event HexBoxEditEventHandler Edited;
        #endregion

        #region Handlers
        private void OnScrolling(object sender, ScrollEventArgs e)
        {
            Scroll();
        }

        private void EditKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                EndEditing(true);
            } else
            {
                switch (e.KeyChar)
                {
                    case 'ф':
                    case 'Ф':
                    case 'a':
                        e.KeyChar = 'A';
                        break;
                    case 'и':
                    case 'И':
                    case 'b':
                        e.KeyChar = 'B';
                        break;
                    case 'с':
                    case 'С':
                    case 'c':
                        e.KeyChar = 'C';
                        break;
                    case 'в':
                    case 'В':
                    case 'd':
                        e.KeyChar = 'D';
                        break;
                    case 'у':
                    case 'У':
                    case 'e':
                        e.KeyChar = 'E';
                        break;
                    case 'а':
                    case 'А':
                    case 'f':
                        e.KeyChar = 'F';
                        break;
                }

                e.Handled = "0123456789ABCDEF\b".IndexOf(e.KeyChar) == -1;
            }
        }
        #endregion

        #region Utils
        private Brush GetBrush(Color color)
        {
            return new SolidBrush(Enabled ? color : Color.LightGray);
        }

        private Pen GetPen(Color color)
        {
            return new Pen(Enabled ? color : Color.LightGray);
        }

        private int GetCellBytes()
        {
            return _ViewMode == HexBoxViewMode.Words ? 2 : 1;
        }

        private int GetCellValue(int index)
        {
            int data = _Dump[index++];
            if (_ViewMode == HexBoxViewMode.Words && index < _Dump.Length)
            {
                data |= (_Dump[index] << 8);
            }
            return data;
        }

        private void SetCellValue(int index, int value)
        {
            _Dump[index++] = (byte)(value & 0xFF);
            if (_ViewMode == HexBoxViewMode.Words && index < _Dump.Length)
            {
                _Dump[index] = (byte)(value >> 8);
            }
        }

        private Point GetCellPos(Point p)
        {
            int x = (p.X - (_HeaderLeft + _AddressWidth + _ColumnsDelim)) / _DataCellWidth;
            int y = (p.Y - (_HeaderTop + 4 + _CharHeight)) / _CharHeight;

            if (0 <= x && x < _DataColums && 0 <= y && y < _DataRows)
            {
                return new Point(x, y);
            }

            return new Point(-1, -1);
        }

        private int GetCellIndex(Point p)
        {
            int index = (int)(_Offset + (p.Y * _DataColums + p.X) * GetCellBytes());

            return index < _Dump.Length ? index : -1;
        }

        private void Resume()
        {
            SizeF _CharSize = CreateGraphics().MeasureString("0", Font, 100, _StringFormat);

            _CharWidth = (int)_CharSize.Width;
            _CharHeight = (int)_CharSize.Height;

            _AddressWidth = 9 * _CharWidth;
            _DataCellWidth = (2 * GetCellBytes() + 1) * _CharWidth;

            _DataRows = (Height - (_HeaderTop + 4 + _CharHeight)) / _CharHeight;

            if (ColumnsAuto)
            {
                int a = 2 * _HeaderLeft + _AddressWidth + _ColumnsDelim + _ScrollBar.Width - _CharWidth;
                int b = _DataCellWidth;

                if (_ViewMode == HexBoxViewMode.BytesAscii)
                {
                    a += _ColumnsDelim - _CharWidth;
                    b += 2 * _CharWidth;
                }

                _DataColums = (Width - a) / b;
            }
        }

        private void ResumeScroll()
        {
            if (_Dump != null)
            {
                _LineLength = GetCellBytes() * _DataColums;

                _Lines = _Dump.Length / _LineLength;
                if (_Lines * _LineLength < _Dump.Length)
                {
                    _Lines++;
                }

                _ScrollBar.Enabled = _DataRows < _Lines;
                if (_ScrollBar.Enabled)
                {
                    _ScrollBar.Minimum = 0;
                    _ScrollBar.Maximum = _Lines - _DataRows + 9;
                }
            }
        }

        private void Scroll()
        {
            uint offset = (uint)(_ScrollBar.Value * _LineLength);

            if (offset != _Offset)
            {
                EndEditing();

                _Offset = offset;
                Invalidate();
            }
        }

        private void EndEditing(bool apply = false)
        {
            if (_EditIndex != -1)
            {
                if (apply)
                {
                    int prev = GetCellValue(_EditIndex);
                    int value = Convert.ToUInt16("0" + _Edit.Text, 16);

                    SetCellValue(_EditIndex, value);

                    Edited?.Invoke(this, new HexBoxEditEventArgs(_EditIndex, prev, value));

                    Invalidate();
                }

                _Edit.Hide();
                _EditIndex = -1;
            }
        }
        #endregion

        #region Drawing
        private int DrawHex(Graphics g, Color color, int x, int y, int value, int width)
        {
            string format = "X" + width.ToString();
            string image = value.ToString(format);

            for (int i = 0; i < image.Length; i++, x += _CharWidth)
            {
                g.DrawString(image.Substring(i, 1), Font, GetBrush(color), x, y, _StringFormat);
            }

            return x;
        }

        private void DrawBackground(Graphics g)
        {
            g.Clear(BackColor);
            g.DrawRectangle(Pens.Gray, new Rectangle(0, 0, Width - 1, Height - 1));

            g.DrawString("-Address-", Font, GetBrush(Color.Gray), _HeaderLeft, _HeaderTop, _StringFormat);

            int x = _HeaderLeft + _AddressWidth + _ColumnsDelim;

            int w = 2 * GetCellBytes();
            for (int i = 0; i < _DataColums; i++, x += _CharWidth)
            {
                x = DrawHex(g, Color.Gray, x, _HeaderTop, i, w);
            }

            if (_ViewMode == HexBoxViewMode.BytesAscii)
            {
                int AsciiWidth = 2 * _CharWidth * _DataColums - _CharWidth;

                x = x - _CharWidth + _ColumnsDelim + (AsciiWidth - 5 * _CharWidth) / 2;

                g.DrawString("ASCII", Font, GetBrush(Color.Gray), x, _HeaderTop, _StringFormat);
            }

            int y = _HeaderTop + _CharHeight + 2;
            w = _HeaderLeft + _AddressWidth + _ColumnsDelim + _DataCellWidth * _DataColums - _CharWidth;
            if (_ViewMode == HexBoxViewMode.BytesAscii)
            {
                w += _ColumnsDelim + 2 * _CharWidth * _DataColums - _CharWidth;
            }
            g.DrawLine(GetPen(Color.Gray), _HeaderLeft, y, w, y);
        }

        private void DrawLines(Graphics g)
        {
            int offset = (int)_Offset;
            int y = _HeaderTop + 4;
            int bytes = GetCellBytes();

            for (int row = 0; row < _DataRows; row++)
            {
                y += _CharHeight;

                int x = DrawHex(g, Color.Blue, _HeaderLeft, y, _ViewMode == HexBoxViewMode.Words ? (int)(offset + _AddressOffset) / 2 : (int)(offset + _AddressOffset), 8);
                g.DrawString(":", Font, GetBrush(Color.Blue), x, y, _StringFormat);

                int xd = _HeaderLeft + _AddressWidth + _ColumnsDelim;
                int xa = xd + _DataColums * _DataCellWidth - _CharWidth + _ColumnsDelim;

                for (int i = 0; i < _DataColums; i++, xd += _CharWidth)
                {
                    int data = GetCellValue(offset);

                    xd = DrawHex(g, Color.Black, xd, y, data, 2 * bytes);

                    if (_ViewMode == HexBoxViewMode.BytesAscii)
                    {
                        char c = _CharConverter.ToChar(_Dump, offset);

                        if (c == '\0')
                        {
                            //g.DrawRectangle(GetPen(Color.DarkMagenta), xa, y+3, _CharWidth, _CharHeight-3);
                            g.DrawString(".", Font, GetBrush(Color.DarkMagenta), xa, y, _StringFormat);
                        } else
                        {
                            g.DrawString(c.ToString(), Font, GetBrush(Color.DarkMagenta), xa, y, _StringFormat);
                        }

                        xa += 2 * _CharWidth;
                    }

                    offset += bytes;
                    if (offset >= _Dump.Length)
                    {
                        row = _DataRows;
                        break;
                    }
                }
            }
        }
        #endregion

        #region Override
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawBackground(e.Graphics);
            if (_Dump != null && _Dump.Length > 0)
            {
                DrawLines(e.Graphics);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            Resume();
            ResumeScroll();

            if (_Offset > 0)
            {
                int dl = _Lines - _DataRows;

                if (dl < (_Offset / _LineLength))
                {
                    EndEditing();

                    _Offset = (dl * _LineLength);
                    _ScrollBar.Value = dl;
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (_ScrollBar.Enabled)
            {
                if (e.Delta < 0)
                {
                    if ((_ScrollBar.Maximum - _ScrollBar.Value) > 9)
                    {
                        _ScrollBar.Value++;
                        Scroll();
                    }
                } else
                {
                    if (_ScrollBar.Value > 0)
                    {
                        _ScrollBar.Value--;
                        Scroll();
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            EndEditing(true);

            if (!Focused)
            {
                Focus();
            }

            Point p = GetCellPos(e.Location);

            if (p.X != -1)
            {
                _EditIndex = GetCellIndex(p);

                if (_EditIndex != -1)
                {
                    p.X = _HeaderLeft + _AddressWidth + _ColumnsDelim + p.X * _DataCellWidth;
                    p.Y = _HeaderTop + 4 + _CharHeight + p.Y * _CharHeight;

                    _Edit.Text = GetCellValue(_EditIndex).ToString("X" + (2 * GetCellBytes()).ToString());
                    _Edit.Location = p;
                    _Edit.Show();
                    _Edit.Select();
                    _Edit.SelectAll();
                }
            }
        }
        #endregion
    }
}
