using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ScePSX.CdRom;

#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8618

namespace ScePSX
{
    public class GameInfo
    {
        public string Name
        {
            get; set;
        }
        public Bitmap Icon
        {
            get; set;
        }
        public long Size
        {
            get; set;
        }
        public string ID
        {
            get; set;
        }
        public string FileName
        {
            get; set;
        }
        public string fullName
        {
            get; set;
        }
        public string LastPlayed
        {
            get; set;
        }
        public bool HasSaveState
        {
            get; set;
        }
        public bool HasCheats
        {
            get; set;
        }
        public string StateText
        {
            get;
            set;
        }
        public string CheatText
        {
            get;
            set;
        }

        public bool IsLastPlayedVisible => !string.IsNullOrWhiteSpace(LastPlayed);
        private string _backgroundColor = "#2A2A2A";
        public string BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
            }
        }

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

        public static Action<GameInfo>? OnDbClick;

        //private bool? _shouldSearchSubdirectories = null;
        private CDData? cddata;
        private List<string> addedfiles;
        private CancellationTokenSource? cts;
        Bitmap DefaultIcon;

        const string Color1 = "#2A2A2A";
        const string Color2 = "#333333";

        public IEnumerable<GameInfo> Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public RomList()
        {
            InitializeComponent();

            GameListBox.Bind(ListBox.ItemsSourceProperty, this.GetObservable(ItemsProperty));
        }

        public void Init()
        {
            DefaultIcon = new Bitmap(AHelper.RootPath + "/icon.png");

            FillByini();
        }

        private void ListBoxItem_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                if (item.DataContext is GameInfo game)
                {
                    OnDbClick?.Invoke(game);
                }
            }
        }

        public Bitmap GetIconBitmap(MemCardMange.SlotData Slot, int idx)
        {
            int width = 16;
            int height = 16;

            using (var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul))
            {
                using (var framebuffer = bitmap.Lock())
                {
                    unsafe
                    {
                        uint* ptr = (uint*)framebuffer.Address.ToPointer();

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int index = y * width + x;

                                Color color = Slot.IconData[idx][index];

                                int pixelIndex = y * framebuffer.RowBytes / 4 + x;

                                ptr[pixelIndex] = (uint)color.ToArgb();
                            }
                        }
                    }
                }

                return bitmap;
            }
        }

        public void FillByini()
        {
            GameListBox.Items.Clear();
            string[] ids = PSXHandler.ini.GetSectionKeys("history");
            if (File.Exists(AHelper.DownloadPath+"/gamedb.yaml"))
                SimpleYaml.ParseYamlFile(AHelper.DownloadPath + "/gamedb.yaml");
            foreach (string id in ids)
            {
                if (id != "")
                {
                    string[] infos = PSXHandler.ini.Read("history", id).Split('|');
                    if (!File.Exists(infos[0]))
                        continue;
                    GameInfo game = FindOrNew(id);
                    game.ID = id;
                    game.fullName = infos[0];
                    game.Name = SimpleYaml.TryGetValue($"{id}.name").Replace("\"", "");
                    if (game.Name == "")
                        game.Name = Path.GetFileNameWithoutExtension(infos[0]);
                    game.FileName = Path.GetFileName(infos[0]);
                    game.Size = new FileInfo(infos[0]).Length;
                    game.LastPlayed = infos[1];
                    game.HasSaveState = Directory.GetFiles(AHelper.RootPath+"/SaveState/", $"{id}_Save?.dat").Length > 0;
                    game.HasCheats = File.Exists(AHelper.RootPath + $"/Cheats/{id}.txt");
                    game.StateText = Translations.GetText("RomList_statesave");
                    game.CheatText = Translations.GetText("RomList_cheat");
                    if (File.Exists(AHelper.RootPath + $"/Icons/{id}.png"))
                    {
                        game.Icon = new Bitmap(AHelper.RootPath + $"/Icons/{id}.png");
                    } else if (File.Exists(AHelper.RootPath + $"/Save/{id}.dat"))
                    {
                        MemCardMange mcr = new MemCardMange(AHelper.RootPath + $"/Save/{id}.dat");
                        foreach (var Slot in mcr.Slots)
                        {
                            if (Slot.IconFrames > 0)
                            {
                                game.Icon = GetIconBitmap(Slot, 0);
                                game.Icon.Save(AHelper.RootPath + $"/Icons/{id}.png");
                                break;
                            }
                        }
                    }
                    if (game.Icon == null)
                        game.Icon = DefaultIcon;
                    int index = GameListBox.Items.IndexOf(game);
                    if (index == -1)
                        index = GameListBox.Items.Count;
                    game.BackgroundColor = (index % 2 == 0) ? Color2 : Color1;
                    AddOrReplace(game);
                }
            }
            SimpleYaml.Clear();
            SortByLastPlayed();
        }

        public void AddByFile(FileInfo f, CDData cddata)
        {
            string ext = Path.GetExtension(f.FullName);

            cddata.DiskID = "";
            cddata.LoadDisk(f.FullName);

            if (cddata.DiskID != "")
            {
                string id = cddata.DiskID;

                GameInfo game = FindOrNew(id);

                game.fullName = f.FullName;
                game.Name = SimpleYaml.TryGetValue($"{id}.name").Replace("\"", "");
                if (game.Name == "")
                    game.Name = Path.GetFileNameWithoutExtension(f.FullName);
                game.FileName = Path.GetFileName(f.FullName);
                game.ID = id;
                if (cddata.Disk.Tracks != null)
                    game.Size = cddata.EndOfDisk;

                string infos = PSXHandler.ini.Read("history", id);

                if (infos == "")
                {
                    game.LastPlayed = "";
                    PSXHandler.ini.Write("history", id, $"{f.FullName}|");
                } else
                {
                    string[] infoary = infos.Split('|');
                    game.LastPlayed = infoary[1];
                }

                game.HasSaveState = Directory.GetFiles(AHelper.RootPath + "/SaveState/", $"{id}_Save?.dat").Length > 0;
                game.HasCheats = File.Exists(AHelper.RootPath + $"/Cheats/{id}.txt");

                if (File.Exists(AHelper.RootPath + "/Icons/{id}.png"))
                {
                    game.Icon = new Bitmap(AHelper.RootPath + $"/Icons/{id}.png");
                } else if (File.Exists(AHelper.RootPath + $"/Save/{id}.dat"))
                {
                    MemCardMange mcr = new MemCardMange(AHelper.RootPath + $"/Save/{id}.dat");
                    foreach (var Slot in mcr.Slots)
                    {
                        if (Slot.IconFrames > 0)
                        {
                            game.Icon = GetIconBitmap(Slot, 0);
                            game.Icon.Save(AHelper.RootPath + $"/Icons/{id}.png");
                            break;
                        }
                    }
                }
                if (game.Icon == null)
                    game.Icon = DefaultIcon;
                int index = GameListBox.Items.IndexOf(game);
                if (index == -1)
                    index = GameListBox.Items.Count;
                game.BackgroundColor = (index % 2 == 0) ? Color2 : Color1;
                AddOrReplace(game);
            }
        }

        public async Task SearchDir(string dir, bool first = true)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();

            await Task.Run(() => SearchDirTask(dir, first, cts.Token), cts.Token);
        }

        private void ProcessFiles(FileInfo[] files, CancellationToken cancellationToken)
        {
            foreach (var f in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (addedfiles.Contains(f.FullName))
                    continue;

                AddByFile(f, cddata);

                if (cddata.Disk.Tracks.Count > 0)
                {
                    foreach (var track in cddata.Disk.Tracks)
                    {
                        addedfiles.Add(track.FilePath);
                    }
                }
            }
        }

        public void SearchDirTask(string dir, bool first, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(dir))
                return;

            if (first)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    addedfiles = new List<string>();
                    cddata = new CDData();
                    if (File.Exists(AHelper.DownloadPath + "/gamedb.yaml"))
                        SimpleYaml.ParseYamlFile(AHelper.DownloadPath + "/gamedb.yaml");
                });
            }
            OSD.Show($"Searching {dir} ...", 99999);
            try
            {
                DirectoryInfo dirinfo = new DirectoryInfo(dir);

                ProcessFiles(dirinfo.GetFiles("*.cue"), cancellationToken);

                foreach (var f in dirinfo.GetFiles())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (f.Extension == ".cue")
                        continue;
                    if (f.Extension != ".bin" && f.Extension != ".iso" && f.Extension != ".img")
                        continue;
                    if (f.Length < 100 * 1024)
                        continue;
                    if (addedfiles.Contains(f.FullName))
                        continue;

                    AddByFile(f, cddata);

                    if (cddata.Disk.Tracks.Count > 0)
                    {
                        foreach (var track in cddata.Disk.Tracks)
                        {
                            addedfiles.Add(track.FilePath);
                        }
                    }
                }

                var subDirectories = dirinfo.GetDirectories();
                if (subDirectories.Length > 0)
                {
                    //TODO Add MessageBox
                    foreach (var subDir in subDirectories)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        SearchDirTask(subDir.FullName, false, cancellationToken);
                    }
                }
            } catch (OperationCanceledException)
            {
            } finally
            {
                if (first)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        SortByLastPlayed();
                        //SimpleOSD.Show(this, "Search Done!", 2000, Color.DarkBlue);
                    });
                }
            }
        }

        private void AddOrReplace(GameInfo game)
        {
            int id = GameListBox.Items.IndexOf(game);
            if (id > -1)
                GameListBox.Items[id] = game;
            else
                GameListBox.Items.Add(game);
        }

        private GameInfo FindOrNew(string id)
        {
            foreach (GameInfo game in GameListBox.Items)
            {
                if (game.ID == id)
                    return game;
            }
            return new GameInfo();
        }

        public void SortByLastPlayed()
        {
            List<GameInfo> sortedGames = new List<GameInfo>();
            foreach (GameInfo game in GameListBox.Items)
            {
                sortedGames.Add(game);
            }

            sortedGames.Sort((x, y) => DateTime.Compare(
                string.IsNullOrEmpty(y.LastPlayed) ? DateTime.MinValue : DateTime.Parse(y.LastPlayed),
                string.IsNullOrEmpty(x.LastPlayed) ? DateTime.MinValue : DateTime.Parse(x.LastPlayed)
            ));

            GameListBox.Items.Clear();
            //foreach (GameInfo game in sortedGames)
            //{
            //    GameListBox.Items.Add(game);
            //}
            for (int i = 0; i < sortedGames.Count; i++)
            {
                sortedGames[i].BackgroundColor = (i % 2 == 0) ? Color2 : Color1;
                GameListBox.Items.Add(sortedGames[i]);
            }
        }
    }
}
