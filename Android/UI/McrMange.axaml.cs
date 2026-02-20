using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

#pragma warning disable CS8618

namespace ScePSX
{
    public partial class McrMangeFrm : UserControl
    {
        private string Title;
        private string ID;
        private MemCardMange card1, card2;
        public ObservableCollection<SaveSlotItem> SaveSlots1 { get; } = new();
        public ObservableCollection<SaveSlotItem> SaveSlots2 { get; } = new();

        public McrMangeFrm()
        {
            InitializeComponent();

            DataContext = this;

            lv1.ItemsSource = SaveSlots1;
            lv2.ItemsSource = SaveSlots2;
        }

        public McrMangeFrm(string id) : this()
        {
            ID = id;

            card1 = new MemCardMange(AHelper.RootPath + $"/Save/{id}.dat");
            card2 = new MemCardMange(AHelper.RootPath + "/Save/MemCard2.dat");

            FillListView(lv1, card1, SaveSlots1);
            FillListView(lv2, card2, SaveSlots2);

            UpdateSlotCount();

            DirectoryInfo dir = new DirectoryInfo(AHelper.RootPath + "/Save");
            if (dir.Exists)
            {
                foreach (FileInfo f in dir.GetFiles())
                {
                    if (Path.GetExtension(f.Name) == ".dat")
                    {
                        string name = Path.GetFileNameWithoutExtension(f.Name);
                        cbsave1.Items.Add(name);
                        cbsave2.Items.Add(name);

                        if (name == id)
                        {
                            cbsave1.SelectedItem = name;
                        }
                        if (name == "MemCard2")
                        {
                            cbsave2.SelectedItem = name;
                        }
                    }
                }
            }

            cbsave1.SelectionChanged += Cbsave1_SelectionChanged;
            cbsave2.SelectionChanged += Cbsave2_SelectionChanged;

            btnimport1.Click += Btnimport1_Click;
            btnimport2.Click += Btnimport2_Click;

            btnformat1.Click += Btnformat1_Click;
            btnformat2.Click += Btnformat2_Click;

            move1to2.Click += Move1to2_Click;
            move2to1.Click += Move2to1_Click;
            copy1to2.Click += Copy1to2_Click;
            copy2to1.Click += Copy2to1_Click;

            del1.Click += Del1_Click;
            del2.Click += Del2_Click;
            out1.Click += Out1_Click;
            out2.Click += Out2_Click;
            save1.Click += Save1_Click;
            save2.Click += Save2_Click;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            Title = Translations.GetText("FrmMcrMange", "forms");
            labmcrtitle.Text = Title;
        }

        private void Btnformat2_Click(object? sender, RoutedEventArgs e)
        {
            card2.FormatCard();
            labStatus.Text = $"{Translations.GetText("cardformat")}-2";
        }

        private void Btnformat1_Click(object? sender, RoutedEventArgs e)
        {
            card1.FormatCard();
            labStatus.Text = $"{Translations.GetText("cardformat")}-1";
        }

        private Bitmap GetIconBitmap(MemCardMange.SlotData Slot, int idx)
        {
            int width = 16;
            int height = 16;

            var writeableBitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

            using (var framebuffer = writeableBitmap.Lock())
            {
                unsafe
                {
                    uint* ptr = (uint*)framebuffer.Address;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = y * width + x;
                            var color = Slot.IconData[idx][index];
                            uint pixel = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
                            ptr[y * width + x] = pixel;
                        }
                    }
                }
            }

            return writeableBitmap;
        }

        private void FillListView(ListBox listBox, MemCardMange card, ObservableCollection<SaveSlotItem> slots)
        {
            slots.Clear();

            for (int i = 0; i < MemCardMange.MaxSlot; i++)
            {
                var slot = card.Slots[i];
                if (slot != null && slot.type == MemCardMange.SlotTypes.initial)
                {
                    var icon = GetIconBitmap(slot, 0);

                    slots.Add(new SaveSlotItem
                    {
                        SlotNumber = i.ToString(),
                        Name = slot.Name,
                        Icon = icon,
                        Size = "15KB",
                        SlotData = slot,
                        SlotIndex = i
                    });
                }
            }
        }

        private void UpdateSlotCount()
        {
            txtSlotCount1.Text = $"{SaveSlots1.Count}/15";
            txtSlotCount2.Text = $"{SaveSlots2.Count}/15";
        }

        private async void Cbsave1_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (cbsave1.SelectedItem == null)
                return;

            string selectedFile = cbsave1.SelectedItem.ToString() ?? "";

            if (cbsave2.SelectedItem != null && selectedFile == cbsave2.SelectedItem.ToString())
            {
                await AHelper.ShowDialog(Translations.GetText("SameName"), Title ?? "");
                cbsave1.SelectedItem = null;
                return;
            }

            card1 = new MemCardMange(AHelper.RootPath + $"/Save/{selectedFile}.dat");
            FillListView(lv1, card1, SaveSlots1);
            UpdateSlotCount();
            labStatus.Text = $"{Translations.GetText("cardload")} {selectedFile}.dat";
        }

        private async void Cbsave2_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (cbsave2.SelectedItem == null)
                return;

            string selectedFile = cbsave2.SelectedItem.ToString() ?? "";

            if (cbsave1.SelectedItem != null && selectedFile == cbsave1.SelectedItem.ToString())
            {
                await AHelper.ShowDialog(Translations.GetText("SameName"), Title ?? "");
                cbsave2.SelectedItem = null;
                return;
            }

            card2 = new MemCardMange(AHelper.RootPath + $"/Save/{selectedFile}.dat");
            FillListView(lv2, card2, SaveSlots2);
            UpdateSlotCount();
            labStatus.Text = $"{Translations.GetText("cardload")} {selectedFile}.dat";
        }

        private async void Move1to2_Click(object? sender, RoutedEventArgs e)
        {
            if (lv1.SelectedItem is not SaveSlotItem selected)
                return;

            int slotNumber = int.Parse(selected.SlotNumber);
            byte[] saveBytes = card1.GetSaveBytes(slotNumber);

            if (card2.AddSaveBytes(slotNumber, saveBytes))
            {
                card1.DeleteSlot(slotNumber);
                FillListView(lv1, card1, SaveSlots1);
                FillListView(lv2, card2, SaveSlots2);
                UpdateSlotCount();
                labStatus.Text = $"{Translations.GetText("cardmove")} {selected.Name}";
            } else
            {
                await AHelper.ShowDialog(Translations.GetText("enoughSlot"), Title ?? "");
            }
        }

        private async void Move2to1_Click(object? sender, RoutedEventArgs e)
        {
            if (lv2.SelectedItem is not SaveSlotItem selected)
                return;

            int slotNumber = int.Parse(selected.SlotNumber);
            byte[] saveBytes = card2.GetSaveBytes(slotNumber);

            if (card1.AddSaveBytes(slotNumber, saveBytes))
            {
                card2.DeleteSlot(slotNumber);
                FillListView(lv1, card1, SaveSlots1);
                FillListView(lv2, card2, SaveSlots2);
                UpdateSlotCount();
                labStatus.Text = $"{Translations.GetText("cardmove")} {selected.Name}";
            } else
            {
                await AHelper.ShowDialog(Translations.GetText("enoughSlot"), Title ?? "");
            }
        }

        private async void Copy1to2_Click(object? sender, RoutedEventArgs e)
        {
            if (lv1.SelectedItem is not SaveSlotItem selected)
                return;

            int slotNumber = int.Parse(selected.SlotNumber);
            byte[] saveBytes = card1.GetSaveBytes(slotNumber);

            if (card2.AddSaveBytes(slotNumber, saveBytes))
            {
                FillListView(lv2, card2, SaveSlots2);
                UpdateSlotCount();
                labStatus.Text = $"{Translations.GetText("cardcopy")} {selected.Name}";
            } else
            {
                await AHelper.ShowDialog(Translations.GetText("enoughSlot"), Title ?? "");
            }
        }

        private async void Copy2to1_Click(object? sender, RoutedEventArgs e)
        {
            if (lv2.SelectedItem is not SaveSlotItem selected)
                return;

            int slotNumber = int.Parse(selected.SlotNumber);
            byte[] saveBytes = card2.GetSaveBytes(slotNumber);

            if (card1.AddSaveBytes(slotNumber, saveBytes))
            {
                FillListView(lv1, card1, SaveSlots1);
                UpdateSlotCount();
                labStatus.Text = $"{Translations.GetText("cardcopy")} {selected.Name}";
            } else
            {
                await AHelper.ShowDialog(Translations.GetText("enoughSlot"), Title ?? "");
            }
        }

        private void Del1_Click(object? sender, RoutedEventArgs e)
        {
            if (lv1.SelectedItem is not SaveSlotItem selected)
                return;

            int slotNumber = int.Parse(selected.SlotNumber);
            card1.DeleteSlot(slotNumber);
            FillListView(lv1, card1, SaveSlots1);
            UpdateSlotCount();
            labStatus.Text = $"{Translations.GetText("carddele")} {selected.Name}";
        }

        private void Del2_Click(object? sender, RoutedEventArgs e)
        {
            if (lv2.SelectedItem is not SaveSlotItem selected)
                return;

            int slotNumber = int.Parse(selected.SlotNumber);
            card2.DeleteSlot(slotNumber);
            FillListView(lv2, card2, SaveSlots2);
            UpdateSlotCount();
            labStatus.Text = $"{Translations.GetText("carddele")} {selected.Name}";
        }

        private async void Out1_Click(object? sender, RoutedEventArgs e)
        {
            var file = await AHelper.SelectFile(Translations.GetText("cardexport"), $"{ID}.mcr", false, AHelper.CommonActivity);
            if (File.Exists(file))
            {
                card1.SaveCard(file);
                labStatus.Text = $"{Translations.GetText("cardexport")} {Path.GetFileName(file)}";
            }
        }

        private async void Out2_Click(object? sender, RoutedEventArgs e)
        {
            var file = await AHelper.SelectFile(Translations.GetText("cardexport"), "MemCard2.mcr", false, AHelper.CommonActivity);
            if (File.Exists(file))
            {
                card2.SaveCard(file);
                labStatus.Text = $"{Translations.GetText("cardexport")} {Path.GetFileName(file)}";
            }
        }

        private void Save1_Click(object? sender, RoutedEventArgs e)
        {
            if (cbsave1.SelectedItem == null)
                return;

            string selectedFile = cbsave1.SelectedItem.ToString() ?? "None";
            card1.SaveCard(AHelper.RootPath + $"/Save/{selectedFile}.dat");
            labStatus.Text = $"{Translations.GetText("cardsave")} {selectedFile}.dat";
        }

        private void Save2_Click(object? sender, RoutedEventArgs e)
        {
            if (cbsave2.SelectedItem == null)
                return;

            string selectedFile = cbsave2.SelectedItem.ToString() ?? "None";
            card2.SaveCard(AHelper.RootPath + $"/Save/{selectedFile}.dat");
            labStatus.Text = $"{Translations.GetText("cardsave")} {selectedFile}.dat";
        }

        private async void Btnimport1_Click(object? sender, RoutedEventArgs e)
        {
            await ImportCard(1);
        }

        private async void Btnimport2_Click(object? sender, RoutedEventArgs e)
        {
            await ImportCard(2);
        }

        private async Task ImportCard(int cardNo)
        {
            string filePath = await AHelper.SelectFile("Memory Card", "", true, AHelper.CommonActivity);
            if (File.Exists(filePath))
            {
                var mcr = new MemCardMange(filePath);
                if (mcr != null)
                {
                    if (cardNo == 1)
                    {
                        card1 = mcr;
                        FillListView(lv1, card1, SaveSlots1);
                    } else
                    {
                        card2 = mcr;
                        FillListView(lv2, card2, SaveSlots2);
                    }
                    UpdateSlotCount();
                    labStatus.Text = $"{Translations.GetText("cardimport")} {Path.GetFileName(filePath)}";
                }
            }
        }
    }

    public class SaveSlotItem
    {
        public string SlotNumber
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        public Bitmap Icon
        {
            get; set;
        }
        public string Size
        {
            get; set;
        }
        public MemCardMange.SlotData SlotData
        {
            get; set;
        }
        public int SlotIndex
        {
            get; set;
        }
    }
}
