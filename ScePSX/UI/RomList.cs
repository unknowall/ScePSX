using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using ScePSX.CdRom2;
using static Khronos.Platform;
using static ScePSX.UI.RomList;

namespace ScePSX.UI
{
    public class RomList : ListBox
    {
        public class Game
        {
            public string Name;
            public Image Icon;
            public long Size;
            public string ID;
            public string FileName;
            public string fullName;
            public string LastPlayed;
            public bool HasSaveState;
            public bool HasCheats;

            public Game()
            {
            }

            public Game(string name, Image icon, long size, string id, string filename, string lastplayed, bool state, bool cheats)
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

        public List<Game> Games = new List<Game> { };

        private readonly Image DefaultIcon;

        public RomList()
        {
            InitializeComponent();

            SetStyle(ControlStyles.DoubleBuffer, true);
            DoubleBuffered = true;

            DrawMode = DrawMode.OwnerDrawVariable;
            BackColor = Color.FromArgb(45, 45, 45);
            ForeColor = Color.White;
            ItemHeight = 85;

            DoubleBuffered = true;

            DefaultIcon = GetDefaultExeIcon();
        }

        public void FillByini()
        {
            string[] ids = FrmMain.ini.GetSectionKeys("history");
            foreach (string id in ids)
            {
                if (id != "")
                {
                    string[] infos = FrmMain.ini.Read("history", id).Split('|');

                    Game game = FindOrNew(id);
                    game.ID = id;
                    game.fullName = infos[0];
                    game.Name = Path.GetFileNameWithoutExtension(infos[0]);
                    game.FileName = Path.GetFileName(infos[0]);
                    game.Size = new FileInfo(infos[0]).Length;
                    game.LastPlayed = infos[1];
                    game.HasSaveState = Directory.GetFiles("./SaveState/", $"{id}_Save?.dat").Length > 0;
                    game.HasCheats = File.Exists($"./Cheats/{id}.txt");
                    if (File.Exists($"./Icons/{id}.png"))
                    {
                        game.Icon = Bitmap.FromFile($"./Icons/{id}.png");
                    } else if (File.Exists($"./Save/{id}.dat"))
                    {
                        //Bitmap bmp = MCR.GetGameIcon($"./Save/{id}.dat", id);
                        //if (bmp != null)
                        //{
                        //    game.Icon = bmp;
                        //    bmp.Save($"./Icons/{id}.png", ImageFormat.Png);
                        //}
                    }
                    AddOrReplace(game);
                }
            }
        }

        public void SearchDir(string dir)
        {
            DirectoryInfo dirinfo = new DirectoryInfo(dir);
            foreach (FileInfo f in dirinfo.GetFiles())
            {
                string ext = Path.GetExtension(f.FullName);
                CDData cddata = new CDData(f.FullName);
                if (cddata.DiskID != "")
                {
                    string id = cddata.DiskID;

                    Game game = FindOrNew(id);

                    game.fullName = f.FullName;
                    game.Name = Path.GetFileNameWithoutExtension(f.FullName);
                    game.FileName = Path.GetFileName(f.FullName);
                    game.ID = id;
                    game.Size = cddata.tracks[0].FileLength;

                    string infos = FrmMain.ini.Read("history", id);

                    if (infos == "")
                    {
                        game.LastPlayed = "";

                    } else
                    {
                        string[] infoary = infos.Split('|');
                        game.LastPlayed = infoary[1];
                    }

                    game.HasSaveState = Directory.GetFiles("./SaveState/", $"{id}_Save?.dat").Length > 0;
                    game.HasCheats = File.Exists($"./Cheats/{id}.txt");

                    if (File.Exists($"./Icons/{id}.png"))
                    {
                        game.Icon = Bitmap.FromFile($"./Icons/{id}.png");
                    } else if (File.Exists($"./Save/{id}.dat"))
                    {
                        //Bitmap bmp = MCR.GetGameIcon($"./Save/{id}.dat", id);
                        //if (bmp != null)
                        //{
                        //    game.Icon = bmp;
                        //    bmp.Save($"./Icons/{id}.png", ImageFormat.Png);
                        //}
                    }

                    AddOrReplace(game);
                }
            }
        }

        private void AddOrReplace(Game game)
        {
            int id = Items.IndexOf(game);
            if (id > -1)
                Items[id] = game;
            else
                Items.Add(game);
        }

        private Game FindOrNew(string id)
        {
            foreach (Game game in Items)
            {
                if (game.ID == id)
                    return game;
            }
            return new Game();
        }

        public Game SelectedGame()
        {
            if (SelectedIndex > -1)
                return Items[SelectedIndex] as Game;
            return null;
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            FormattingEnabled = true;
            TabIndex = 0;
            Name = "RomList";
            Size = new System.Drawing.Size(510, 316);
            ResumeLayout(false);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            // 背景
            e.Graphics.FillRectangle(new SolidBrush(this.BackColor), e.Bounds);

            if (e.Index < 0 || e.Index >= this.Items.Count)
                return;

            var game = this.Items[e.Index] as Game;

            Rectangle bounds = e.Bounds;
            int iconSize = 48;
            int padding = 5;

            // 大框（暗色风格）
            using (var borderPen = new Pen(Color.FromArgb(100, 100, 100), 2)) // 边框颜色
            using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) // 半透明阴影
            using (var mainBrush = new SolidBrush(Color.FromArgb(60, 60, 60))) // 主框背景颜色
            {
                // 阴影
                e.Graphics.FillRectangle(shadowBrush, bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
                // 主框
                e.Graphics.FillRectangle(mainBrush, bounds.X, bounds.Y, bounds.Width - 2, bounds.Height - 2);
                e.Graphics.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width - 2, bounds.Height - 2);
            }

            // 图标（靠左）
            Image iconToDraw = game.Icon ?? DefaultIcon;
            int icony = bounds.Top + (bounds.Height - iconSize) / 2;
            if (iconToDraw != null)
            {
                e.Graphics.DrawImage(iconToDraw, bounds.Left + padding, icony, iconSize, iconSize);
            }

            // 名称
            using (var nameFont = new Font("Arial", 13, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                string name = game.Name;
                SizeF nameSize = e.Graphics.MeasureString(name, nameFont);
                e.Graphics.DrawString(name, nameFont, brush, bounds.Left + iconSize + padding * 2, icony + 3);
            }

            // 名称下面的信息
            int startX = bounds.Left + iconSize + 15;
            int startY = bounds.Top + 32;

            DrawInfoBox(e.Graphics, $"{game.ID}", startX, startY + 13, 8);

            // 靠右下的信息
            startX = bounds.Right - 340;
            startY = bounds.Bottom - 32;
            if (game.LastPlayed != "")
                DrawInfoBox(e.Graphics, $"最后运行: {game.LastPlayed}", startX - 26, startY, 9);
            DrawInfoBox(e.Graphics, $"即时存档: {(game.HasSaveState ? "✓" : "✗")}", startX + 166, startY, 9);
            DrawInfoBox(e.Graphics, $"金手指: {(game.HasCheats ? "✓" : "✗")}", startX + 260, startY, 9);

            //DrawInfoBox(e.Graphics, $"{game.FileName}", startX, startY, 8); // {game.Size / 1024} KB

            // 选中效果
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                using (var focusPen = new Pen(Color.Orange, 2)) // 使用橙色边框表示选中
                {
                    e.Graphics.DrawRectangle(focusPen, bounds.X + 1, bounds.Y + 1, bounds.Width - 3, bounds.Height - 3);
                }
            }
        }

        private void DrawInfoBox(Graphics g, string label, int x, int y, int fontSize = 9)
        {
            using (var boxBrush = new SolidBrush(Color.FromArgb(50, 50, 50))) // 框背景颜色
            using (var borderPen = new Pen(Color.FromArgb(100, 100, 100))) // 边框颜色
            using (var textBrush = new SolidBrush(Color.White)) // 文字颜色
            using (var font = new Font("Arial", fontSize))
            {
                int padding = 4;

                SizeF labelSize = g.MeasureString(label, font);

                // 绘制框
                g.FillRectangle(boxBrush, x, y, labelSize.Width + padding * 2, labelSize.Height + padding * 2);
                g.DrawRectangle(borderPen, x, y, labelSize.Width + padding * 2, labelSize.Height + padding * 2);

                // 绘制文字
                g.DrawString(label, font, textBrush, x + padding, y + padding);
            }
        }

        private void DrawInfoBoxValue(Graphics g, string label, string value, int x, int y, int width, int height, int fontsize = 9)
        {
            using (var boxBrush = new SolidBrush(Color.LightSlateGray))
            using (var borderPen = new Pen(Color.Gray))
            using (var textBrush = new SolidBrush(Color.White))
            using (var font = new Font("Arial", fontsize))
            {
                SizeF labelSize = g.MeasureString(label, font);
                SizeF valueSize = g.MeasureString(value, font);

                g.FillRectangle(boxBrush, x, y, labelSize.Width + 2, labelSize.Height + 2);
                g.DrawRectangle(borderPen, x, y, labelSize.Width + 2, labelSize.Height + 2);

                g.DrawString(label, font, textBrush, x + 2, y + (height - labelSize.Height) / 2);

                g.DrawString(value, font, textBrush, x + width - valueSize.Width - 2, y + (height - valueSize.Height) / 2);
            }
        }

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            e.ItemHeight = ItemHeight;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate();
        }

        private Image GetDefaultExeIcon()
        {
            try
            {
                Icon defaultIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                return defaultIcon.ToBitmap();
            } catch
            {
                return new Bitmap(48, 48);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DefaultIcon.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                return (createParams);
            }
        }
    }

    public class MCR
    {
        private const int BLOCK_SIZE = 8192;
        private const int HEADER_OFFSET = 0x0A;
        private const int PALETTE_OFFSET = 0x60;
        private const int PIXELS_OFFSET = 0x80;
        private const int ID_LENGTH = 12;

        public static Bitmap GetGameIcon(string memoryCardPath, string targetGameId)
        {
            using var fs = new FileStream(memoryCardPath, FileMode.Open, FileAccess.Read);
            byte[] blockBuffer = new byte[BLOCK_SIZE];

            byte[] header = new byte[2];
            fs.Read(header, 0, 2);
            bool isStandardFormat = (header[0] == 0x4D && header[1] == 0x43); // "MC"

            if (isStandardFormat)
                fs.Position = 128;

            for (int block = 0; block < 15; block++)
            {
                fs.Read(blockBuffer, 0, BLOCK_SIZE);

                string fullId = Encoding.ASCII.GetString(blockBuffer, HEADER_OFFSET, ID_LENGTH).Trim('\0', ' ');
                string shortId = fullId.StartsWith("BI") ? fullId.Substring(2) : fullId;

                if (shortId.Replace('-', '_') == targetGameId)
                {
                    return ExtractIcon(blockBuffer);
                }
            }
            return null;
        }

        private static Bitmap ExtractIcon(byte[] blockBuffer)
        {
            //todo
            return null;
        }

    }

}
