using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ScePSX.UI
{
    public class GameInfo
    {
        public string Name { get; set; }
        public Bitmap Icon { get; set; }
        public long Size { get; set; }
        public string ID { get; set; }
        public string FileName { get; set; }
        public string fullName { get; set; }
        public string LastPlayed { get; set; }
        public bool HasSaveState { get; set; }
        public bool HasCheats { get; set; }

        public GameInfo()
        {
        }

        public GameInfo(string name, Bitmap icon, long size, string id, string filename, string lastplayed, bool state, bool cheats)
        {
            Name = name;
            Icon = icon;
            Size = size;
            ID = id;
            FileName = filename;
            LastPlayed = lastplayed;
            HasSaveState = state;
            HasCheats = cheats;
        }
    }

    public partial class RomList : UserControl
    {
        public static readonly StyledProperty<IEnumerable<GameInfo>> ItemsProperty =
            AvaloniaProperty.Register<RomList, IEnumerable<GameInfo>>(nameof(Items));

        public IEnumerable<GameInfo> Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public RomList()
        {
            InitializeComponent();
            // 绑定ListBox的ItemsSource到Items
            GameListBox.Bind(ListBox.ItemsSourceProperty, this.GetObservable(ItemsProperty));

            for (int i = 0; i< 20; i++)
                GameListBox.Items.Add(new GameInfo($"testetestset - {i}", new Bitmap("./001.ico"), 0, $"ULUS-000{i}", "", "", true, true));
        }
    }
}
