using System;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ScePSX.UI;

public class HexBox : Control
{
    public static readonly StyledProperty<byte[]> MemoryProperty =
        AvaloniaProperty.Register<HexBox, byte[]>(nameof(Memory), Array.Empty<byte>());

    public byte[] Memory
    {
        get => GetValue(MemoryProperty);
        set => SetValue(MemoryProperty, value);
    }

    public static readonly StyledProperty<ulong> BaseAddressProperty =
        AvaloniaProperty.Register<HexBox, ulong>(nameof(BaseAddress), 0x80000000UL);

    public ulong BaseAddress
    {
        get => GetValue(BaseAddressProperty);
        set => SetValue(BaseAddressProperty, value);
    }

    public static readonly StyledProperty<bool> ShowScrollBarProperty =
        AvaloniaProperty.Register<HexBox, bool>(nameof(ShowScrollBar), true);

    public bool ShowScrollBar
    {
        get => GetValue(ShowScrollBarProperty);
        set => SetValue(ShowScrollBarProperty, value);
    }

    private const int BYTES_PER_ROW = 16;
    private const int ROW_HEIGHT = 22;
    private const int ADDRESS_WIDTH = 80;
    private const int HEX_COLUMN_WIDTH = 28;
    private const int ASCII_COLUMN_WIDTH = 16;
    private const int CELL_PADDING = 4;
    private const int SCROLLBAR_WIDTH = 13;

    private int _visibleRows;
    private int _scrollOffset;
    private EditCell? _editingCell;
    private bool _isDraggingScroll = false;
    private Point _lastMousePosition;

    public enum Encoding
    {
        ASCII,
        UTF8,
        UNICODE
    }
    public Encoding encoding = Encoding.ASCII;

    private class EditCell
    {
        public int Row
        {
            get; set;
        }
        public int Column
        {
            get; set;
        }
        public string InputBuffer { get; set; } = "";
        public int InputIndex { get; set; } = 0;
    }

    static HexBox()
    {
        AffectsRender<HexBox>(MemoryProperty, BaseAddressProperty, ShowScrollBarProperty);
        FocusableProperty.OverrideDefaultValue<HexBox>(true);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = (Memory.Length + BYTES_PER_ROW - 1) / BYTES_PER_ROW * ROW_HEIGHT;
        return new Size(availableSize.Width, Math.Min(height, availableSize.Height));
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _visibleRows = (int)(Bounds.Height / ROW_HEIGHT);
        InvalidateVisual();
    }

    public void NavigateTo(ulong address)
    {
        if (Memory == null || Memory.Length == 0)
            return;

        int relativeAddress;
        if (address >= BaseAddress)
        {
            relativeAddress = (int)(address - BaseAddress);
        } else
        {
            relativeAddress = (int)address; // 如果小于BaseAddress，视为相对地址
        }

        relativeAddress = Math.Max(0, Math.Min(relativeAddress, Memory.Length - 1));

        int targetRow = relativeAddress / BYTES_PER_ROW;
        int totalRows = (Memory.Length + BYTES_PER_ROW - 1) / BYTES_PER_ROW;
        int maxScroll = Math.Max(0, totalRows - _visibleRows);

        _scrollOffset = Math.Max(0, Math.Min(targetRow, maxScroll));

        InvalidateVisual();
    }

    public void NavigateTo(int address)
    {
        NavigateTo((ulong)address);
    }

    private string DecodeRowBytes(byte[] rowBytes)
    {
        switch (encoding)
        {
            case Encoding.UTF8:
                return DecodeUTF8(rowBytes);
            case Encoding.UNICODE:
                return DecodeUnicode(rowBytes);
            case Encoding.ASCII:
            default:
                return DecodeASCII(rowBytes);
        }
    }

    private string DecodeASCII(byte[] bytes)
    {
        var chars = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            chars[i] = b < 32 || b > 127 ? '.' : (char)b;
        }
        return new string(chars);
    }

    private string DecodeUTF8(byte[] bytes)
    {
        try
        {
            string result = System.Text.Encoding.UTF8.GetString(bytes);

            var sb = new StringBuilder();
            foreach (char c in result)
            {
                if (char.IsControl(c) || c > 127)
                    sb.Append('.');
                else
                    sb.Append(c);
            }
            return sb.ToString().PadRight(bytes.Length).Substring(0, bytes.Length);
        } catch
        {
            return DecodeASCII(bytes);
        }
    }

    private string DecodeUnicode(byte[] bytes)
    {
        try
        {
            // Unicode (UTF-16 LE)
            if (bytes.Length < 2)
                return DecodeASCII(bytes);

            int length = bytes.Length - (bytes.Length % 2);
            byte[] unicodeBytes = new byte[length];
            Array.Copy(bytes, unicodeBytes, length);

            string result = System.Text.Encoding.Unicode.GetString(unicodeBytes);

            var sb = new StringBuilder();
            foreach (char c in result)
            {
                if (char.IsControl(c) || c > 127)
                    sb.Append('.');
                else
                    sb.Append(c);
            }

            string decoded = sb.ToString();
            if (decoded.Length < BYTES_PER_ROW / 2)
                decoded = decoded.PadRight(BYTES_PER_ROW / 2);
            else if (decoded.Length > BYTES_PER_ROW / 2)
                decoded = decoded.Substring(0, BYTES_PER_ROW / 2);

            return decoded;
        } catch
        {
            return DecodeASCII(bytes);
        }
    }

    public override void Render(DrawingContext context)
    {
        if (Memory == null || Memory.Length == 0)
            return;

        var typeface = new Typeface("Courier New");
        var emSize = 12.0;
        var whiteBrush = new SolidColorBrush(Colors.White);
        var lightGrayBrush = new SolidColorBrush(Colors.LightGray);
        var grayBrush = new SolidColorBrush(Colors.Gray);
        var yellowBrush = new SolidColorBrush(Colors.Yellow);
        var darkBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        var bgBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        var scrollBarBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        var scrollBarThumbBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
        var grayPen = new Pen(grayBrush, 1);
        var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(100, 100, 100)), 1);

        // 计算内容宽度
        //double contentWidth = ADDRESS_WIDTH + BYTES_PER_ROW * HEX_COLUMN_WIDTH + 10 + BYTES_PER_ROW * ASCII_COLUMN_WIDTH;
        double availableWidth = Bounds.Width - (ShowScrollBar ? SCROLLBAR_WIDTH : 0);

        // 画背景
        context.FillRectangle(bgBrush, new Rect(0, 0, Bounds.Width, Bounds.Height));

        // 画表头 - Offset
        var offsetText = new FormattedText(
            "Offset",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            emSize,
            whiteBrush);
        context.DrawText(offsetText, new Point(5, 2));

        // 画表头 - HEX列索引
        for (int i = 0; i < BYTES_PER_ROW; i++)
        {
            var hexHeaderText = new FormattedText(
                i.ToString("X2"),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                emSize,
                whiteBrush);
            var x = ADDRESS_WIDTH + i * HEX_COLUMN_WIDTH;
            context.DrawText(hexHeaderText, new Point(x + CELL_PADDING, 2));
        }

        // 画表头
        var codeing = "ASCII";
        switch (encoding)
        {
            case Encoding.ASCII:
                codeing = "ASCII";
                break;
            case Encoding.UTF8:
                codeing = "UTF8";
                break;
            case Encoding.UNICODE:
                codeing = "UNICODE";
                break;
        }

        var asciiHeaderText = new FormattedText(
            codeing,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            emSize,
            whiteBrush);
        context.DrawText(asciiHeaderText,
            new Point(ADDRESS_WIDTH + BYTES_PER_ROW * HEX_COLUMN_WIDTH + 10, 2));

        // 画分隔线
        context.DrawLine(grayPen, new Point(0, 20), new Point(Bounds.Width, 20));

        // 画数据行
        int startRow = _scrollOffset;
        int endRow = Math.Min(startRow + _visibleRows + 1,
            (Memory.Length + BYTES_PER_ROW - 1) / BYTES_PER_ROW);

        for (int row = startRow; row < endRow; row++)
        {
            int y = 25 + (row - startRow) * ROW_HEIGHT;
            int baseIndex = row * BYTES_PER_ROW;

            // 画地址
            ulong address = BaseAddress + (ulong)baseIndex;
            var addrText = new FormattedText(
                $"0x{address:X8}",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                emSize,
                whiteBrush);
            context.DrawText(addrText, new Point(5, y));

            // 画HEX
            for (int col = 0; col < BYTES_PER_ROW; col++)
            {
                int index = baseIndex + col;
                if (index >= Memory.Length)
                    break;

                int x = ADDRESS_WIDTH + col * HEX_COLUMN_WIDTH;
                var rect = new Rect(x, y - 2, HEX_COLUMN_WIDTH - 2, ROW_HEIGHT - 2);

                if (_editingCell != null && _editingCell.Row == row && _editingCell.Column == col)
                {
                    // 编辑状态
                    context.FillRectangle(darkBrush, rect);
                    var editText = new FormattedText(
                        _editingCell.InputBuffer.PadRight(2).Substring(0, 2),
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        emSize,
                        yellowBrush);
                    context.DrawText(editText, new Point(x + CELL_PADDING, y));
                } else
                {
                    // 正常显示
                    var hexText = new FormattedText(
                        Memory[index].ToString("X2"),
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        emSize,
                        whiteBrush);
                    context.DrawText(hexText, new Point(x + CELL_PADDING, y));
                }
            }

            int textX = ADDRESS_WIDTH + BYTES_PER_ROW * HEX_COLUMN_WIDTH + 10;

            byte[] rowBytes = new byte[BYTES_PER_ROW];
            int bytesInRow = 0;
            for (int col = 0; col < BYTES_PER_ROW; col++)
            {
                int index = baseIndex + col;
                if (index >= Memory.Length)
                    break;
                rowBytes[col] = Memory[index];
                bytesInRow++;
            }

            if (bytesInRow < BYTES_PER_ROW)
            {
                Array.Resize(ref rowBytes, bytesInRow);
            }

            string decodedText = DecodeRowBytes(rowBytes);

            var textDisplay = new FormattedText(
                decodedText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                emSize,
                whiteBrush);

            context.DrawText(textDisplay, new Point(textX, y));
        }

        // 画滚动条
        if (ShowScrollBar)
        {
            DrawScrollBar(context, scrollBarBrush, scrollBarThumbBrush, borderPen);
        }
    }

    private void DrawScrollBar(DrawingContext context, IBrush scrollBarBrush, IBrush thumbBrush, IPen borderPen)
    {
        double scrollBarX = Bounds.Width - SCROLLBAR_WIDTH;
        double scrollBarHeight = Bounds.Height - 20; // 减去表头高度

        // 滚动条背景
        var scrollBarRect = new Rect(scrollBarX, 20, SCROLLBAR_WIDTH, scrollBarHeight);
        context.FillRectangle(scrollBarBrush, scrollBarRect);
        context.DrawRectangle(borderPen, scrollBarRect);

        // 计算滚动块位置
        int totalRows = (Memory.Length + BYTES_PER_ROW - 1) / BYTES_PER_ROW;
        int maxScroll = Math.Max(0, totalRows - _visibleRows);

        if (maxScroll > 0)
        {
            double thumbHeight = Math.Max(20, scrollBarHeight * _visibleRows / totalRows);
            double thumbY = 20 + (scrollBarHeight - thumbHeight) * _scrollOffset / maxScroll;

            var thumbRect = new Rect(scrollBarX + 2, thumbY, SCROLLBAR_WIDTH - 4, thumbHeight);
            context.FillRectangle(thumbBrush, thumbRect);
        } else
        {
            // 不需要滚动时显示一个较小的滚动块
            var thumbRect = new Rect(scrollBarX + 2, 20, SCROLLBAR_WIDTH - 4, 20);
            context.FillRectangle(thumbBrush, thumbRect);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetPosition(this);

        if (ShowScrollBar && point.X >= Bounds.Width - SCROLLBAR_WIDTH)
        {
            _isDraggingScroll = true;
            _lastMousePosition = point;
            UpdateScrollFromPoint(point);
            e.Handled = true;
            return;
        }

        var (row, col, isHex) = HitTest(point);

        if (row >= 0 && col >= 0 && isHex)
        {
            int index = (row + _scrollOffset) * BYTES_PER_ROW + col;
            if (index < Memory.Length)
            {
                _editingCell = new EditCell
                {
                    Row = row + _scrollOffset,
                    Column = col,
                    InputBuffer = Memory[index].ToString("X2"),
                    InputIndex = 0
                };
                InvalidateVisual();
            }
        } else
        {
            _editingCell = null;
            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDraggingScroll)
        {
            var point = e.GetPosition(this);
            _lastMousePosition = point;
            UpdateScrollFromPoint(point);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDraggingScroll)
        {
            _isDraggingScroll = false;
            e.Handled = true;
        }
    }

    private void UpdateScrollFromPoint(Point point)
    {
        if (Memory == null || Memory.Length == 0)
            return;

        double scrollBarY = point.Y - 20; // 减去表头高度
        double scrollBarHeight = Bounds.Height - 20;

        int totalRows = (Memory.Length + BYTES_PER_ROW - 1) / BYTES_PER_ROW;
        int maxScroll = Math.Max(0, totalRows - _visibleRows);

        if (maxScroll > 0)
        {
            double thumbHeight = Math.Max(20, scrollBarHeight * _visibleRows / totalRows);
            double normalizedY = Math.Max(0, Math.Min(scrollBarY, scrollBarHeight - thumbHeight));

            _scrollOffset = (int)(normalizedY / (scrollBarHeight - thumbHeight) * maxScroll);
            _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, maxScroll));

            InvalidateVisual();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_editingCell == null)
            return;

        if (e.Key == Key.Enter)
        {
            // 提交编辑
            if (_editingCell.InputBuffer.Length > 0)
            {
                int index = _editingCell.Row * BYTES_PER_ROW + _editingCell.Column;
                if (index < Memory.Length)
                {
                    if (byte.TryParse(_editingCell.InputBuffer,
                        System.Globalization.NumberStyles.HexNumber, null, out byte b))
                    {
                        Memory[index] = b;
                        // 触发内存更新事件
                        RaiseEvent(new MemoryChangedEventArgs(MemoryChangedEvent, index, b));
                    }
                }
            }
            _editingCell = null;
            InvalidateVisual();
            e.Handled = true;
        } else if (e.Key == Key.Escape)
        {
            _editingCell = null;
            InvalidateVisual();
            e.Handled = true;
        } else if (e.Key == Key.Back)
        {
            if (_editingCell.InputBuffer.Length > 0)
            {
                _editingCell.InputBuffer = _editingCell.InputBuffer[0..^1];
                _editingCell.InputIndex--;
                InvalidateVisual();
            }
            e.Handled = true;
        } else
        {
            // 十六进制输入
            var key = e.Key.ToString();
            if (key.Length == 2)
                key = key.Replace("D", "");
            if (key.Length == 1 && "0123456789ABCDEF".Contains(key.ToUpper()))
            {
                if (_editingCell.InputIndex < 2)
                {
                    if (_editingCell.InputIndex == 0)
                    {
                        if (_editingCell.InputBuffer.Length > 1)
                            _editingCell.InputBuffer = key.ToUpper() + _editingCell.InputBuffer[1];
                        else
                            _editingCell.InputBuffer = key.ToUpper();
                    } else
                        _editingCell.InputBuffer = _editingCell.InputBuffer[0] + key.ToUpper();

                    _editingCell.InputIndex++;
                    InvalidateVisual();
                }
                e.Handled = true;
            }
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var delta = e.Delta.Y;
        _scrollOffset = Math.Max(0,
            Math.Min(_scrollOffset - (delta > 0 ? 1 : -1),
                Math.Max(0, (Memory.Length + BYTES_PER_ROW - 1) / BYTES_PER_ROW - _visibleRows)));

        InvalidateVisual();
        e.Handled = true;
    }

    private (int row, int col, bool isHex) HitTest(Point point)
    {
        if (point.Y < 25)
            return (-1, -1, false);

        int row = (int)((point.Y - 25) / ROW_HEIGHT);
        if (row < 0 || row >= _visibleRows + 1)
            return (-1, -1, false);

        // HEX区
        if (point.X >= ADDRESS_WIDTH && point.X <= ADDRESS_WIDTH + BYTES_PER_ROW * HEX_COLUMN_WIDTH)
        {
            int col = (int)((point.X - ADDRESS_WIDTH) / HEX_COLUMN_WIDTH);
            if (col >= 0 && col < BYTES_PER_ROW)
            {
                return (row, col, true);
            }
        }

        return (row, -1, false);
    }

    // 内存改变事件
    public static readonly RoutedEvent<MemoryChangedEventArgs> MemoryChangedEvent =
        RoutedEvent.Register<HexBox, MemoryChangedEventArgs>(nameof(MemoryChanged), RoutingStrategies.Bubble);

    public event EventHandler<MemoryChangedEventArgs> MemoryChanged
    {
        add => AddHandler(MemoryChangedEvent, value);
        remove => RemoveHandler(MemoryChangedEvent, value);
    }
}

public class MemoryChangedEventArgs : RoutedEventArgs
{
    public int Address
    {
        get;
    }
    public byte Value
    {
        get;
    }

    public MemoryChangedEventArgs(RoutedEvent routedEvent, int address, byte value) : base(routedEvent)
    {
        Address = address;
        Value = value;
    }
}
