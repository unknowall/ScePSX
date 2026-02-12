using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ScePSX.UI;

public class HexBox : Control
{
    // 依赖属性
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

    // 配置参数
    private const int BYTES_PER_ROW = 16;
    private const int ROW_HEIGHT = 22;
    private const int ADDRESS_WIDTH = 80;
    private const int HEX_COLUMN_WIDTH = 28;
    private const int ASCII_COLUMN_WIDTH = 16;
    private const int CELL_PADDING = 4;

    private int _visibleRows;
    private int _scrollOffset;
    private EditCell? _editingCell;

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
    }

    static HexBox()
    {
        AffectsRender<HexBox>(MemoryProperty, BaseAddressProperty);
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
        _visibleRows = (int)(Bounds.Height / ROW_HEIGHT) + 1;
        InvalidateVisual();
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

        // 画表头 - ASCII
        var asciiHeaderText = new FormattedText(
            "ASCII",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            emSize,
            whiteBrush);
        context.DrawText(asciiHeaderText,
            new Point(ADDRESS_WIDTH + BYTES_PER_ROW * HEX_COLUMN_WIDTH + 10, 2));

        // 画分隔线
        context.DrawLine(new Pen(grayBrush, 1), new Point(0, 20), new Point(Bounds.Width, 20));

        // 画数据行
        int startRow = _scrollOffset;
        int endRow = Math.Min(startRow + _visibleRows,
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
                lightGrayBrush);
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

            // 画ASCII
            int asciiX = ADDRESS_WIDTH + BYTES_PER_ROW * HEX_COLUMN_WIDTH + 10;
            var asciiChars = new char[BYTES_PER_ROW];
            for (int col = 0; col < BYTES_PER_ROW; col++)
            {
                int index = baseIndex + col;
                if (index >= Memory.Length)
                {
                    asciiChars[col] = ' ';
                    continue;
                }

                byte b = Memory[index];
                asciiChars[col] = b < 32 || b > 127 ? '.' : (char)b;
            }

            var asciiText = new FormattedText(
                new string(asciiChars),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                emSize,
                whiteBrush);
            context.DrawText(asciiText, new Point(asciiX, y));
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetPosition(this);
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
                    InputBuffer = Memory[index].ToString("X2")
                };
                InvalidateVisual();
            }
        } else
        {
            _editingCell = null;
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
                InvalidateVisual();
            }
            e.Handled = true;
        } else
        {
            // 十六进制输入
            var key = e.Key.ToString();
            if (key.Length == 1 && "0123456789ABCDEF".Contains(key.ToUpper()))
            {
                if (_editingCell.InputBuffer.Length < 2)
                {
                    _editingCell.InputBuffer += key.ToUpper();
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
        if (row < 0 || row >= _visibleRows)
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
