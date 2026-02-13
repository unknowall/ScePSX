using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ScePSX.UI
{
    public partial class CheatFrm : Window
    {
        public ObservableCollection<CheatItem> CheatItems { get; } = new();
        public List<PSXCore.CheatCode> cheatCodes = new();

        private PSXCore? Core;
        private string DiskID;

        public CheatFrm()
        {
            InitializeComponent();
        }

        public CheatFrm(string id, PSXCore? core) : this()
        {
            DataContext = this;

            clb.ItemsSource = CheatItems;
            ctb.LostFocus += UpdateCodes;
            ctb.TextChanged += (s, e) => UpdateCodes(s, e);

            Core = core;

            if (string.IsNullOrEmpty(id))
            {
                BtnSave.IsEnabled = false;
                btnload.IsEnabled = false;
                btnapply.IsEnabled = false;
                BtnSearchCheat.IsEnabled = false;
            }

            DiskID = id;
            labDiskID.Text = $"🎮 {DiskID}";
            Title = $"{Title} - {DiskID}";

            BtnLoad_Click(null, null);
        }

        private void BtnAdd_Click(object? sender, RoutedEventArgs? e)
        {
            var newItem = new CheatItem
            {
                Name = Translations.GetText("newcheatitem"),
                IsChecked = false,
                CodeCount = 0
            };
            CheatItems.Add(newItem);
            clb.SelectedItem = newItem;
            ctb.Text = "";
        }

        private void BtnDel_Click(object sender, RoutedEventArgs e)
        {
            if (clb.SelectedItem is CheatItem selected)
            {
                CheatItems.Remove(selected);
                ctb.Clear();
            }
        }

        private void UpdateCodes(object? sender, EventArgs e)
        {
            if (clb.SelectedItem is CheatItem selected)
            {
                selected.CodeText = ctb.Text ?? "";

                var lines = ctb.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                selected.CodeCount = lines.Length;
            }
        }

        private void UpdateCheatList()
        {
            CheatItems.Clear();
            foreach (var item in cheatCodes)
            {
                string codes = "";
                foreach (var sitem in item.Item)
                {
                    codes += $"{sitem.Address:X8} {sitem.Value:X4}\r\n";
                }

                var lines = codes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                CheatItems.Add(new CheatItem
                {
                    Name = item.Name,
                    IsChecked = item.Active,
                    CodeText = codes.TrimEnd(),
                    CodeCount = lines.Length
                });
            }

            UpdateStatus($"{Translations.GetText("loadcheatitem")}  {CheatItems.Count}");
        }

        private void BtnLoad_Click(object? sender, RoutedEventArgs? e)
        {
            cheatCodes.Clear();
            string fn = $"./Cheats/{DiskID}.txt";

            if (!File.Exists(fn))
            {
                return;
            }

            cheatCodes = PSXCore.ParseTextToCheatCodeList(fn);
            UpdateCheatList();
            UpdateStatus($"{Translations.GetText("loadcheatitem")}  {cheatCodes.Count}");
        }

        private void BtnSave_Click(object? sender, RoutedEventArgs? e)
        {
            string fn = $"./Cheats/{DiskID}.txt";
            string txt = GetText();

            Directory.CreateDirectory("./Cheats/");
            File.WriteAllText(fn, txt);

            UpdateStatus(Translations.GetText("saved"));
        }

        private async Task<string> SelectFile()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return "";
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "CheatCode",
                FileTypeFilter = new[]
                {
            new FilePickerFileType("Games")
            {
                Patterns = new[] { "*.txt", "*.cht" }
            }
        },
                SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(PSXHandler.ini.Read("main", "LastPath"))
            });
            if (files == null || files.Count == 0)
                return "";
            var filePath = files[0].Path.LocalPath;
            if (!File.Exists(filePath))
                return "";
            return filePath;
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var result = await SelectFile();

            if (result.Length > 0)
            {
                try
                {
                    cheatCodes = PSXCore.ParseTextToCheatCodeList(result);
                    UpdateCheatList();
                    UpdateStatus($"{Translations.GetText("cheatimport")} {Path.GetFileName(result)}");
                } catch (Exception ex)
                {
                    UpdateStatus($"{Translations.GetText("importerror")}  {ex.Message}");
                }
            }
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (Core == null)
            {
                return;
            }

            BtnSave_Click(sender, e);
            Core.LoadCheats();

            UpdateStatus(Translations.GetText("cheatapply"));
        }

        private void Clb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (clb.SelectedItem is CheatItem selected)
            {
                ctb.Text = selected.CodeText ?? "";
            }
        }

        private void Clb_DbCLick(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (clb.SelectedItem is not CheatItem selectedItem)
                return;

            if (sender is ListBoxItem item)
            {
                item.IsSelected = true;
            }

            var listBoxItem = clb.ContainerFromItem(selectedItem) as ListBoxItem;
            if (listBoxItem == null)
                return;

            var textBox = listBoxItem.FindDescendantOfType<TextBox>();
            if (textBox != null)
            {
                textBox.IsReadOnly = false;
                textBox.BorderThickness = new Thickness(1);
                textBox.BorderBrush = new SolidColorBrush(Color.Parse("#FF0078D4"));
                textBox.Background = new SolidColorBrush(Color.Parse("#FFF0F7FF"));

                Dispatcher.UIThread.Post(() =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }, DispatcherPriority.Background);
            }
        }

        private void EditName_LostFocus(object? sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.IsReadOnly = true;
                textBox.BorderThickness = new Thickness(0);
                textBox.Background = Brushes.Transparent;
            }
        }

        private void EditName_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null || !textBox.IsReadOnly)
                return;

            if (e.Key == Avalonia.Input.Key.Enter)
            {
                textBox.IsReadOnly = true;
                textBox.BorderThickness = new Thickness(0);
                textBox.Background = Brushes.Transparent;
                e.Handled = true;
            } else if (e.Key == Avalonia.Input.Key.Escape)
            {
                if (textBox.DataContext is CheatItem item)
                {
                    textBox.Text = item.Name;
                    textBox.IsReadOnly = true;
                    textBox.BorderThickness = new Thickness(0);
                    textBox.Background = Brushes.Transparent;
                }
                e.Handled = true;
            }
        }

        private string GetText()
        {
            string ret = "";
            foreach (var item in CheatItems)
            {
                ret += $"\r\n[{item.Name}]\r\n";
                ret += $"Active = {(item.IsChecked ? "1" : "0")}\r\n";
                ret += item.CodeText ?? "";
            }
            return ret.TrimStart();
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            const string urlprefix = "http://epsxe.com/cheats/";
            string urlid = ConvertID(DiskID);

            if (string.IsNullOrEmpty(urlid))
            {
                UpdateStatus(Translations.GetText("importerror"));
                return;
            }

            try
            {
                BtnSearchCheat.IsEnabled = false;
                UpdateStatus(Translations.GetText("cheatdownload"));

                string content = await ReadUrlContentAsync(urlprefix + urlid);

                if (string.IsNullOrEmpty(content))
                {
                    UpdateStatus(Translations.GetText("importerror"));
                    return;
                }

                string cheatstr = ConvertToBracketedFormat(content);
                cheatCodes = PSXCore.ParseTextToCheatCodeList(cheatstr, false);
                UpdateCheatList();
                BtnSave_Click(null, null);
                UpdateStatus($"{Translations.GetText("loadcheatitem")}  {cheatCodes.Count}");
            } catch (Exception ex)
            {
                UpdateStatus($"{Translations.GetText("importerror")} {ex.Message}");
            } finally
            {
                BtnSearchCheat.IsEnabled = true;
            }
        }

        private string ConvertID(string input)
        {
            if (!input.Contains("-"))
                return "";

            string[] parts = input.Split('-');
            string prefix = parts[0];
            string number = parts[1];

            string integerPart = number.Substring(0, number.Length - 2);
            string decimalPart = number.Substring(number.Length - 2);

            return $"{prefix}/{prefix}_{integerPart}.{decimalPart}.txt";
        }

        private async Task<string> ReadUrlContentAsync(string url)
        {
            using HttpClient client = new();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            if (response.StatusCode != HttpStatusCode.OK)
                return "";

            return await response.Content.ReadAsStringAsync();
        }

        private string ConvertToBracketedFormat(string input)
        {
            string[] lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> outputLines = new();

            foreach (string line in lines)
            {
                if (line.TrimStart().StartsWith("#"))
                {
                    string content = line.Substring(line.IndexOf('#') + 1).Trim();
                    outputLines.Add($"[{content}]");
                    outputLines.Add("Active = 0");
                } else
                {
                    outputLines.Add(line.Trim());
                }
            }
            return string.Join(Environment.NewLine, outputLines);
        }

        private void UpdateStatus(string message)
        {
            labStatus.Text = message;
        }
    }

    public class CheatItem
    {
        public string Name
        {
            get; set;
        }
        public bool IsChecked
        {
            get; set;
        }
        public string CodeText
        {
            get; set;
        }
        public int CodeCount
        {
            get; set;
        }
    }
}
