using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ScePSX.UI
{
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

    public partial class MemEdit : Window
    {
        private ObservableCollection<MemoryItem> memoryItems = new ObservableCollection<MemoryItem>();
        private bool isAutoRefresh = false;
        private System.Timers.Timer autoRefreshTimer;

        private const long PSX_BASE = 0x80000000;
        private byte[] blankdata = new byte[1024];

        private MemorySearch memsearch;
        private static List<(int Address, object Value)> SearchResults = new List<(int Address, object Value)> { };

        public enum SearchType
        {
            Byte,
            Word,
            DWord,
            Float
        }

        PSXCore? Core;

        public MemEdit()
        {
            InitializeComponent();
        }

        public unsafe MemEdit(PSXCore? core) : this()
        {
            InitComboBoxes();

            Core = core;

            if (Core != null)
            {
                HexBox.Memory = ConvertBytePointerToByteArray(Core.PsxBus.ramPtr, 2048 * 1024);
            } else
            {
                HexBox.Memory = blankdata;
            }
            HexBox.ShowScrollBar = true;

            scanresult.ItemsSource = memoryItems;

            SetupAutoRefreshTimer();

            AttachEvents();
        }

        private unsafe byte[] ConvertBytePointerToByteArray(byte* ptr, int length)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = *(ptr + i);
            }
            return result;
        }

        private void InitComboBoxes()
        {
            CboEncode.ItemsSource = new List<string> { "ASCII", "UTF-8", "Unicode" };
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

            if (CboEncode != null)
                CboEncode.SelectionChanged += CboEncode_SelectedIndexChanged;

            if (HexBox != null)
                HexBox.MemoryChanged += HexBox_Edited;

            if (chkupd != null)
                chkupd.IsCheckedChanged += Chkupd_IsCheckedChanged;

            if (scanresult != null)
            {
                scanresult.DoubleTapped += Ml_DoubleTapped;
            }
        }

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

        private void btngo_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbgoto.Text))
                return;

            try
            {
                string input = tbgoto.Text.Trim();
                if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    input = input.Substring(2);
                }

                int address = Convert.ToInt32(input, 16);

                HexBox.NavigateTo(address);
            } catch
            {
            }
        }

        private unsafe void btnupd_Click(object? sender, RoutedEventArgs? e)
        {
            if (Core != null)
            {
                HexBox.Memory = ConvertBytePointerToByteArray(Core.PsxBus.ramPtr, 2048 * 1024);
            } else
            {
                HexBox.Memory = blankdata;
            }
        }

        private unsafe void btnr_Click(object? sender, RoutedEventArgs e)
        {
            if (Core == null)
                return;

            memsearch = new MemorySearch(ConvertBytePointerToByteArray(Core.PsxBus.ramPtr, 2048 * 1024));

            SearchResults.Clear();

            findb.Text = "";
            memoryItems.Clear();
        }

        private unsafe void btns_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(findb.Text))
            {
                return;
            }

            if (memsearch == null)
                memsearch = new MemorySearch(ConvertBytePointerToByteArray(Core.PsxBus.ramPtr, 2048 * 1024));
            else
                memsearch.UpdateData(ConvertBytePointerToByteArray(Core.PsxBus.ramPtr, 2048 * 1024));

            if (rbbyte.IsChecked ?? false)
            {
                byte tmp;
                if (!byte.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchByte(tmp);
            } else
            if (rbWord.IsChecked ?? false)
            {
                ushort tmp;
                if (!ushort.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchWord(tmp);
            } else
            if (rbDword.IsChecked ?? false)
            {
                uint tmp;
                if (!uint.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchDword(tmp);
            } else
            if (rbfloat.IsChecked ?? false)
            {
                float tmp;
                if (!float.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchFloat(tmp);
            }

            SearchResults = memsearch.GetResults();

            labFirst500.Text = $"{Translations.GetText("Form_Mem_updateml_find")} {SearchResults.Count} {Translations.GetText("Form_Mem_updateml_result")}";

            memoryItems.Clear();
            for (int i = 0; i < SearchResults.Count; i++)
            {
                if (i >= 500)
                    break;

                memoryItems.Add(new MemoryItem
                {
                    Address = $"0x{PSX_BASE + SearchResults[i].Address:X8}",
                    Value = SearchResults[i].Value.ToString() ?? "0"
                });
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

        private void Ml_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            var row = scanresult.SelectedItem as MemoryItem;
            if (row != null)
            {
                try
                {
                    string addrStr = row.Address.Replace("0x", "");
                    int address = Convert.ToInt32(addrStr, 16);
                    HexBox.NavigateTo(address);
                } catch { }
            }
        }

        private unsafe void HexBox_Edited(object? sender, MemoryChangedEventArgs e)
        {
            Core.PsxBus.ramPtr[e.Address - PSX_BASE] = (byte)e.Value;
        }

        private unsafe void WriteMemory(string addressStr, string value)
        {
            try
            {
                addressStr = addressStr.Replace("0x", "");
                uint address = (uint)Convert.ToInt32(addressStr, 16);
                uint tmp = uint.Parse(value);
                address = Core.PsxBus.GetMask(address);
                if (tmp < 0xFF)
                {
                    Core.PsxBus.write(address & 0x1F_FFFF, (byte)tmp, Core.PsxBus.ramPtr);
                } else if (tmp < 0xFFFF)
                {
                    Core.PsxBus.write(address & 0x1F_FFFF, (ushort)tmp, Core.PsxBus.ramPtr);
                } else
                {
                    Core.PsxBus.write(address & 0x1F_FFFF, tmp, Core.PsxBus.ramPtr);
                }
            } catch
            {
            }
        }

        private void CboEncode_SelectedIndexChanged(object? sender, SelectionChangedEventArgs e)
        {
            HexBox.encoding = (HexBox.Encoding)CboEncode.SelectedIndex;
            HexBox.InvalidateVisual();
        }

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

        protected override void OnClosed(EventArgs e)
        {
            autoRefreshTimer?.Stop();
            autoRefreshTimer?.Dispose();
            base.OnClosed(e);
        }
    }
}
