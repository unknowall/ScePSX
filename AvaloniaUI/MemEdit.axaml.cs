using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ScePSX.UI
{
    public partial class MemEdit : Window
    {
        private ObservableCollection<MemoryItem> memoryItems = new ObservableCollection<MemoryItem>();
        private bool isAutoRefresh = false;
        private System.Timers.Timer autoRefreshTimer;

        public MemEdit()
        {
            InitializeComponent();

            InitComboBoxes();

            ml.ItemsSource = memoryItems;

            SetupAutoRefreshTimer();

            AttachEvents();
        }

        private void InitComboBoxes()
        {
            CboView.ItemsSource = new List<string> { "Hex", "Ascii", "Hex+Ascii" };
            CboView.SelectedIndex = 0;

            CboEncode.ItemsSource = new List<string> { "UTF-8", "ASCII", "Unicode" };
            CboEncode.SelectedIndex = 0;
        }

        private void SetupAutoRefreshTimer()
        {
            autoRefreshTimer = new System.Timers.Timer(1000);
            autoRefreshTimer.Elapsed += (s, e) =>
            {
                if (isAutoRefresh)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        btnupd_Click(null, null);
                    });
                }
            };
        }

        private void AttachEvents()
        {
            if (btngo != null)
                btngo.Click += btngo_Click;
            if (btnupd != null)
                btnupd.Click += btnupd_Click;
            if (btnr != null)
                btnr.Click += btnr_Click;
            if (btns != null)
                btns.Click += btns_Click;

            if (tbgoto != null)
                tbgoto.KeyUp += tbgoto_KeyPress;
            if (findb != null)
                findb.KeyUp += findb_KeyPress;

            if (CboView != null)
                CboView.SelectionChanged += CboView_SelectedIndexChanged;
            if (CboEncode != null)
                CboEncode.SelectionChanged += CboEncode_SelectedIndexChanged;

            //if (HexBox != null)
            //    HexBox.Edited += HexBox_Edited;

            if (chkupd != null)
                chkupd.IsCheckedChanged += Chkupd_IsCheckedChanged;

            if (ml != null)
            {
                ml.CellEditEnding += Ml_CellEditEnding;
                ml.DoubleTapped += Ml_DoubleTapped;
            }
        }

        // 自动刷新复选框
        private void Chkupd_IsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            isAutoRefresh = chkupd.IsChecked ?? false;

            if (isAutoRefresh)
            {
                autoRefreshTimer.Start();
            } else
            {
                autoRefreshTimer.Stop();
            }
        }

        // 前往地址
        private void btngo_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbgoto.Text))
                return;

            try
            {
                // 支持十六进制输入（带或不带0x前缀）
                string input = tbgoto.Text.Trim();
                if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    input = input.Substring(2);
                }

                long address = Convert.ToInt64(input, 16);

                //HexBox?.NavigateTo(address);
            } catch
            {
            }
        }

        // 刷新按钮
        private void btnupd_Click(object? sender, RoutedEventArgs? e)
        {
            //HexBox?.Refresh();
        }

        // 重置搜索
        private void btnr_Click(object? sender, RoutedEventArgs e)
        {
            findb.Text = "";
            memoryItems.Clear();
            labFirst500.Text = "搜索到0个地址 只显示前500个";
        }

        // 搜索按钮
        private void btns_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(findb.Text))
            {
                ShowMessage("请输入搜索内容");
                return;
            }

            try
            {
                // 获取搜索类型
                SearchType type = GetSearchType();

                // 解析搜索值
                byte[] searchBytes = ParseSearchValue(findb.Text, type);

                // 执行搜索（这里需要根据你的HexBox实现来写）
                // List<long> results = HexBox?.Search(searchBytes) ?? new List<long>();

                // 模拟搜索结果
                var results = new List<long> { 0x80000000, 0x80000100, 0x80000200 };

                // 更新搜索结果列表
                memoryItems.Clear();
                foreach (var addr in results.Take(500))
                {
                    memoryItems.Add(new MemoryItem
                    {
                        Address = $"0x{addr:X8}",
                        Value = GetValueAtAddress(addr, type)
                    });
                }

                labFirst500.Text = $"搜索到{results.Count}个地址 只显示前500个";
            } catch (Exception ex)
            {
                ShowMessage($"搜索失败: {ex.Message}");
            }
        }

        private SearchType GetSearchType()
        {
            if (rbbyte?.IsChecked == true)
                return SearchType.Byte;
            if (rbWord?.IsChecked == true)
                return SearchType.Word;
            if (rbDword?.IsChecked == true)
                return SearchType.DWord;
            if (rbfloat?.IsChecked == true)
                return SearchType.Float;
            return SearchType.Byte;
        }

        private byte[] ParseSearchValue(string input, SearchType type)
        {
            input = input.Trim();

            switch (type)
            {
                case SearchType.Byte:
                    return new byte[] { byte.Parse(input, System.Globalization.NumberStyles.HexNumber) };
                case SearchType.Word:
                    return BitConverter.GetBytes(ushort.Parse(input, System.Globalization.NumberStyles.HexNumber));
                case SearchType.DWord:
                    return BitConverter.GetBytes(uint.Parse(input, System.Globalization.NumberStyles.HexNumber));
                case SearchType.Float:
                    return BitConverter.GetBytes(float.Parse(input));
                default:
                    return new byte[0];
            }
        }

        private string GetValueAtAddress(long address, SearchType type)
        {
            // 这里应该从HexBox读取实际值
            // 模拟返回值
            switch (type)
            {
                case SearchType.Byte:
                    return "00";
                case SearchType.Word:
                    return "0000";
                case SearchType.DWord:
                    return "00000000";
                case SearchType.Float:
                    return "0.0";
                default:
                    return "00";
            }
        }

        // 单元格编辑
        private void Ml_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "值")
            {
                if (e.EditingElement is TextBox textBox)
                {
                    var row = e.Row.DataContext as MemoryItem;
                    if (row != null && textBox.Text != null)
                    {
                        string newValue = textBox.Text;
                        WriteMemory(row.Address, newValue);
                    }
                }
            }
        }

        // 单元格双击
        private void Ml_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            var row = ml.SelectedItem as MemoryItem;
            if (row != null)
            {
                // 跳转到HexBox对应地址
                try
                {
                    string addrStr = row.Address.Replace("0x", "");
                    long address = Convert.ToInt64(addrStr, 16);
                    //HexBox?.NavigateTo(address);
                } catch { }
            }
        }

        // HexBox编辑事件
        private void HexBox_Edited(object sender, EventArgs e)
        {
            // 刷新搜索结果列表的值
            foreach (var item in memoryItems)
            {
                try
                {
                    string addrStr = item.Address.Replace("0x", "");
                    long address = Convert.ToInt64(addrStr, 16);
                    // item.Value = GetValueFromHexBox(address);
                } catch { }
            }
        }

        private void WriteMemory(string addressStr, string value)
        {
            try
            {
                addressStr = addressStr.Replace("0x", "");
                long address = Convert.ToInt64(addressStr, 16);
                // HexBox?.WriteBytes(address, value);
            } catch (Exception ex)
            {
                ShowMessage($"写入失败: {ex.Message}");
            }
        }

        // ComboBox事件
        private void CboView_SelectedIndexChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (HexBox == null)
                return;

            switch (CboView.SelectedIndex)
            {
                case 0:
                    //HexBox.ViewMode = HexBoxControl.HexBoxViewMode.Hex; // 假设有Hex模式
                    break;
                case 1:
                    //HexBox.ViewMode = HexBoxControl.HexBoxViewMode.Ascii;
                    break;
                case 2:
                    //HexBox.ViewMode = HexBoxControl.HexBoxViewMode.BytesAscii;
                    break;
            }
        }

        private void CboEncode_SelectedIndexChanged(object? sender, SelectionChangedEventArgs e)
        {
            // 切换编码
            // HexBox?.SetEncoding(CboEncode.SelectedIndex);
        }

        // KeyPress事件
        private void tbgoto_KeyPress(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btngo_Click(sender, e);
            }
        }

        private void findb_KeyPress(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btns_Click(sender, e);
            }
        }

        private void ShowMessage(string message)
        {
            // 简单的消息提示
            var messageBox = new Window
            {
                Title = "提示",
                Content = message,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                //Owner = this
            };
            messageBox.ShowDialog(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            autoRefreshTimer?.Stop();
            autoRefreshTimer?.Dispose();
            base.OnClosed(e);
        }
    }

    // 搜索类型枚举
    public enum SearchType
    {
        Byte,
        Word,
        DWord,
        Float
    }

    // 内存项数据模型
    public class MemoryItem
    {
        public string Address
        {
            get; set;
        }
        public string Value
        {
            get; set;
        }
    }
}
