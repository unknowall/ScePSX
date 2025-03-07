using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime;
using System.Windows.Forms;

using ScePSX.CdRom2;

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

        private int _hoverIndex = -1;
        private readonly Image DefaultIcon;
        private ContextMenuStrip contextMenuStrip;

        private Rectangle scrollBarBounds; // 滚动条区域
        private Rectangle thumbBounds;    // 滑块区域
        private bool isDraggingThumb = false; // 是否正在拖动滑块
        private int thumbPosition = 0;    // 滑块当前位置
        private int thumbSize;            // 滑块大小
        private bool _isScrollBarVisible;
        private const int ScrollBarWidth = 8;     // 滚动条总宽度
        private const int ScrollBarMargin = 2;     // 滚动条与边缘间距
        private const int ThumbMinSize = 20;       // 滑块最小高度
        private readonly Color TrackColor = Color.FromArgb(60, 60, 60);    // 轨道颜色
        private readonly Color ThumbColor = Color.FromArgb(100, 100, 100); // 滑块颜色
        private readonly Color ThumbHoverColor = Color.FromArgb(120, 120, 120); // 滑块悬停颜色

        private Color TextColor = Color.White;
        private Color InfoBackColor = Color.FromArgb(50, 50, 50);
        private Color MainBackColor = Color.FromArgb(45, 45, 45);
        private Color MenuBackColor = Color.FromArgb(45, 45, 45);
        private Color ItemBackColor2 = Color.FromArgb(43, 43, 43);
        private Color ItemBackColor1 = Color.FromArgb(50, 50, 50);
        private Color HoverColor = Color.FromArgb(70, 70, 70); // 悬停时的高亮颜色
        private Color SelectionColor = Color.Orange; // 选中时的高亮颜色
        private Color BorderColor = Color.FromArgb(100, 100, 100); // 边框颜色
        private Color ShadowColor = Color.FromArgb(50, 0, 0, 0); // 半透明阴影
        private Color MainBoardColor = Color.FromArgb(60, 60, 60); // 主框背景颜色
        private Color InfoBorderColor = Color.FromArgb(100, 100, 100);

        private static Color MenuSelectColor = Color.FromArgb(70, 70, 70); // 菜单选中颜色
        private static Color MenuHoverColor = Color.FromArgb(80, 80, 80); // 菜单悬停颜色
        private static Color MenuUnSelectColor = Color.FromArgb(45, 45, 45);
        private static Color SepColor = Color.FromArgb(100, 100, 100);

        public RomList()
        {
            InitializeComponent();

            scrollBarBounds = new Rectangle(
                ClientRectangle.Width - ScrollBarWidth - ScrollBarMargin,
                ScrollBarMargin,
                ScrollBarWidth,
                Math.Max(ThumbMinSize * 2, ClientRectangle.Height - 2 * ScrollBarMargin)
            );

            // 初始化滑块尺寸和位置
            thumbSize = scrollBarBounds.Height;
            thumbPosition = 0;

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();

            DrawMode = DrawMode.OwnerDrawVariable;
            BackColor = MainBackColor;
            ForeColor = TextColor;
            ItemHeight = 85;

            DefaultIcon = GetDefaultExeIcon();

            MouseMove += RomList_MouseMove;
            MouseLeave += RomList_MouseLeave;

            // 初始化右键菜单
            contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.RenderMode = ToolStripRenderMode.Professional;
            contextMenuStrip.Renderer = new CustomToolStripRenderer();
            contextMenuStrip.BackColor = MenuBackColor;
            var split = new ToolStripSeparator();

            var item0 = new ToolStripMenuItem(ScePSX.Properties.Resources.RomList_RomList_存档管理, null, OnMcrClick);
            var item1 = new ToolStripMenuItem(ScePSX.Properties.Resources.RomList_RomList_修改设置, null, OnSetClick);
            var item2 = new ToolStripMenuItem(ScePSX.Properties.Resources.RomList_RomList_编辑金手指, null, OnCheatClick);

            var item5 = new ToolStripMenuItem(ScePSX.Properties.Resources.RomList_RomList_取存档图标, null, OnUpIconClick);
            var item3 = new ToolStripMenuItem(ScePSX.Properties.Resources.RomList_RomList_设置图标, null, OnSetIconClick);
            var item4 = new ToolStripMenuItem(ScePSX.Properties.Resources.RomList_RomList_删除, null, OnDeleteClick);
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                item0, split,
                item1, item2, split,
                item5, item3, split, item4
            });
            ContextMenuStrip = contextMenuStrip;
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

        public void FillByini()
        {
            string[] ids = FrmMain.ini.GetSectionKeys("history");
            if (File.Exists("gamedb.yaml"))
                SimpleYaml.ParseYamlFile("gamedb.yaml");
            foreach (string id in ids)
            {
                if (id != "")
                {
                    string[] infos = FrmMain.ini.Read("history", id).Split('|');
                    if (!File.Exists(infos[0]))
                        continue;
                    Game game = FindOrNew(id);
                    game.ID = id;
                    game.fullName = infos[0];
                    game.Name = SimpleYaml.TryGetValue($"{id}.name").Replace("\"", "");
                    if (game.Name == "")
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
                        MemCardMange mcr = new MemCardMange($"./Save/{id}.dat");
                        foreach (var Slot in mcr.Slots)
                        {
                            if (Slot.ProdCode == id && Slot.type == MemCardMange.SlotTypes.initial)
                            {
                                game.Icon = Slot.GetIconBitmap(0);
                                game.Icon.Save($"./Icons/{id}.png", ImageFormat.Png);
                                break;
                            }
                        }
                    }
                    AddOrReplace(game);
                }
            }
            SimpleYaml.Clear();
            SortByLastPlayed();
        }

        public void AddByFile(FileInfo f)
        {
            string ext = Path.GetExtension(f.FullName);
            CDData cddata = new CDData(f.FullName);
            if (cddata.DiskID != "")
            {
                string id = cddata.DiskID;

                Game game = FindOrNew(id);

                game.fullName = f.FullName;
                game.Name = SimpleYaml.TryGetValue($"{id}.name").Replace("\"", "");
                if (game.Name == "")
                    game.Name = Path.GetFileNameWithoutExtension(f.FullName);
                game.FileName = Path.GetFileName(f.FullName);
                game.ID = id;
                game.Size = cddata.tracks[0].FileLength;

                string infos = FrmMain.ini.Read("history", id);

                if (infos == "")
                {
                    game.LastPlayed = "";
                    FrmMain.ini.Write("history", id, $"{f.FullName}|");
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
                    MemCardMange mcr = new MemCardMange($"./Save/{id}.dat");
                    foreach (var Slot in mcr.Slots)
                    {
                        if (Slot.ProdCode == id && Slot.type == MemCardMange.SlotTypes.initial)
                        {
                            game.Icon = Slot.GetIconBitmap(0);
                            game.Icon.Save($"./Icons/{id}.png", ImageFormat.Png);
                            break;
                        }
                    }
                }

                AddOrReplace(game);
            }
        }

        private bool? _shouldSearchSubdirectories = null;

        public void SearchDir(string dir)
        {
            if (File.Exists("gamedb.yaml"))
                SimpleYaml.ParseYamlFile("gamedb.yaml");
            DirectoryInfo dirinfo = new DirectoryInfo(dir);
            foreach (FileInfo f in dirinfo.GetFiles())
            {
                AddByFile(f);
            }
            DirectoryInfo[] subDirectories = dirinfo.GetDirectories();
            if (subDirectories.Length > 0)
            {
                if (_shouldSearchSubdirectories == null)
                {
                    DialogResult result = MessageBox.Show(
                        ScePSX.Properties.Resources.RomList_SearchDir_是否要搜索子目录,
                        "",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    _shouldSearchSubdirectories = result == DialogResult.Yes;
                }

                if (_shouldSearchSubdirectories == true)
                {
                    foreach (DirectoryInfo subDir in subDirectories)
                    {
                        SearchDir(subDir.FullName);
                    }
                }
            }
            SimpleYaml.Clear();
            SortByLastPlayed();
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

        public void SortByLastPlayed()
        {
            List<Game> sortedGames = new List<Game>();
            foreach (Game game in Items)
            {
                sortedGames.Add(game);
            }

            sortedGames.Sort((x, y) => DateTime.Compare(
                string.IsNullOrEmpty(y.LastPlayed) ? DateTime.MinValue : DateTime.Parse(y.LastPlayed),
                string.IsNullOrEmpty(x.LastPlayed) ? DateTime.MinValue : DateTime.Parse(x.LastPlayed)
            ));

            Items.Clear();
            foreach (Game game in sortedGames)
            {
                Items.Add(game);
            }
        }

        private void ShowFrom(Form Frm)
        {
            Frm.StartPosition = FormStartPosition.Manual;
            Frm.Owner = (Form)this.Parent;

            Point parentCenterClient = new Point(
                this.ClientSize.Width / 2,
                this.ClientSize.Height / 2
            );

            Point parentCenterScreen = this.PointToScreen(parentCenterClient);

            Frm.Location = new Point(
                parentCenterScreen.X - Frm.Width / 2,
                parentCenterScreen.Y - Frm.Height / 2
                );

            Frm.Show();
        }

        private void OnSetClick(object sender, EventArgs e)
        {
            ShowFrom(new Form_Set(SelectedGame().ID));
        }

        private void OnMcrClick(object sender, EventArgs e)
        {
            ShowFrom(new Form_McrMange(SelectedGame().ID));
        }

        private void OnUpIconClick(object sender, EventArgs e)
        {
            Game selectedGame = SelectedGame();
            if (selectedGame == null)
                return;

            MemCardMange mcr = new MemCardMange($"./Save/{selectedGame.ID}.dat");
            foreach (var Slot in mcr.Slots)
            {
                if (Slot.ProdCode == selectedGame.ID && Slot.type == MemCardMange.SlotTypes.initial)
                {
                    if (selectedGame.Icon != null)
                        selectedGame.Icon.Dispose();

                    selectedGame.Icon = Slot.GetIconBitmap(0);
                    selectedGame.Icon.Save($"./Icons/{selectedGame.ID}.png", ImageFormat.Png);
                    break;
                }
            }
        }

        private void OnCheatClick(object sender, EventArgs e)
        {
            ShowFrom(new Form_Cheat(SelectedGame().ID));
        }

        private void OnSetIconClick(object sender, EventArgs e)
        {
            Game selectedGame = SelectedGame();
            if (selectedGame != null)
            {
                OpenFileDialog FD = new OpenFileDialog();
                FD.Filter = "Icon|*.png;*.bmp;*.jpg;*.ico";
                FD.ShowDialog();
                if (!File.Exists(FD.FileName))
                    return;

                if (selectedGame.Icon != null)
                    selectedGame.Icon.Dispose();

                using (var tempImage = Image.FromFile(FD.FileName))
                {
                    selectedGame.Icon = new Bitmap(tempImage);
                }
                selectedGame.Icon.Save($"./Icons/{selectedGame.ID}.png", ImageFormat.Png);

                Invalidate();
            }
        }

        private void OnDeleteClick(object sender, EventArgs e)
        {
            Game selectedGame = SelectedGame();
            if (selectedGame != null)
            {
                Items.Remove(selectedGame);
                FrmMain.ini.DeleteKey("history", selectedGame.ID);
            }
        }

        private void RomList_MouseMove(object sender, MouseEventArgs e)
        {
            int index = myIndexFromPoint(e.Location);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                Invalidate();
            }

            if (isDraggingThumb && scrollBarBounds.Contains(e.Location))
            {
                // 拖动滑块
                thumbPosition = Math.Max(0, Math.Min(e.Y - thumbSize / 2, scrollBarBounds.Height - thumbSize));
                UpdateScrollPosition();
                Invalidate();
            }
        }

        private void RomList_MouseLeave(object sender, EventArgs e)
        {
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = myIndexFromPoint(e.Location);

                if (index != ListBox.NoMatches && index >= 0 && index < Items.Count)
                {
                    SelectedIndex = index;
                } else
                {
                    SelectedIndex = -1;
                }

                if (SelectedIndex != -1)
                {
                    contextMenuStrip.Show(this, e.Location);
                }
            }

            if (scrollBarBounds.Contains(e.Location))
            {

                if (thumbBounds.Contains(e.Location))
                {
                    isDraggingThumb = true;
                } else
                {
                    thumbPosition = Math.Max(0, Math.Min(e.Y - thumbSize / 2, scrollBarBounds.Height - thumbSize));
                    UpdateScrollPosition();
                }
                Invalidate();
                return; // 阻止基类处理
            }


            Point adjustedPoint = new Point(
                Math.Min(e.X, ClientSize.Width - scrollBarBounds.Width - 1),
                e.Y
            );
            base.OnMouseDown(new MouseEventArgs(e.Button, e.Clicks, adjustedPoint.X, adjustedPoint.Y, e.Delta));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            isDraggingThumb = false;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count)
                return;

            if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
                return;

            using (var doubleBuffer = new Bitmap(e.Bounds.Width, e.Bounds.Height))
            using (var g = Graphics.FromImage(doubleBuffer))
            {
                var localArgs = new DrawItemEventArgs(
                    g,
                    e.Font,
                    new Rectangle(0, 0, e.Bounds.Width, e.Bounds.Height),
                    e.Index,
                    e.State
                );

                DrawItems(localArgs);

                e.Graphics.DrawImage(doubleBuffer, e.Bounds.Location);
            }
        }

        private void DrawItems(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= this.Items.Count)
                return;

            if (_isScrollBarVisible)
            {
                int contentWidth = ClientSize.Width - scrollBarBounds.Width;
                e = new DrawItemEventArgs(
                    e.Graphics,
                    e.Font,
                    new Rectangle(e.Bounds.X, e.Bounds.Y, contentWidth, e.Bounds.Height), // 修正宽度
                    e.Index,
                    e.State
                );
            }

            Rectangle bounds = e.Bounds;

            bool isHovered = e.Index == _hoverIndex;
            
            Color rowBackColor = (e.Index % 2 == 0)
                ? ItemBackColor2 // 偶数行稍浅
                : ItemBackColor1; // 奇数行稍深

            if (isHovered)
                rowBackColor = HoverColor; 

            using (var backBrush = new SolidBrush(rowBackColor))
            {
                e.Graphics.FillRectangle(backBrush, bounds);
            }

            var game = this.Items[e.Index] as Game;

            int iconSize = 48;
            int padding = 5;

            DrawMainBox(e.Graphics, bounds);

            DrawIcon(e.Graphics, game.Icon ?? DefaultIcon, bounds, iconSize, padding);

            DrawName(e.Graphics, game.Name, bounds, iconSize, padding);

            DrawInfoBoxes(e.Graphics, game, bounds, iconSize, padding);

            //if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            //{
            //    DrawSelectionEffect(e.Graphics, bounds);
            //}
        }

        private void DrawMainBox(Graphics g, Rectangle bounds)
        {
            using (var borderPen = new Pen(BorderColor, 2)) // 边框颜色
            using (var shadowBrush = new SolidBrush(ShadowColor)) // 半透明阴影
            //using (var mainBrush = new SolidBrush(MainBoardColor)) // 主框背景颜色
            {
                // 阴影
                g.FillRectangle(shadowBrush, bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
                // 主框
                //g.FillRectangle(mainBrush, bounds.X, bounds.Y, bounds.Width - 2, bounds.Height - 2);
                g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width - 2, bounds.Height - 2);
            }
        }

        private void DrawIcon(Graphics g, Image icon, Rectangle bounds, int iconSize, int padding)
        {
            int icony = bounds.Top + (bounds.Height - iconSize) / 2;
            if (icon != null)
            {
                g.DrawImage(icon, bounds.Left + padding, icony, iconSize, iconSize);
            }
        }

        private void DrawName(Graphics g, string name, Rectangle bounds, int iconSize, int padding)
        {
            using (var nameFont = new Font("Arial", 13, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                int icony = bounds.Top + (bounds.Height - iconSize) / 2;
                SizeF nameSize = g.MeasureString(name, nameFont);
                g.DrawString(name, nameFont, brush, bounds.Left + iconSize + padding * 2, icony + 3);
            }
        }

        private void DrawInfoBoxes(Graphics g, Game game, Rectangle bounds, int iconSize, int padding)
        {
            int startX = bounds.Left + iconSize + 15;
            int startY = bounds.Top + 32;

            DrawInfoBox(g, $"{game.ID}", startX, startY + 13, 8);

            startX = bounds.Right - 340;
            startY = bounds.Bottom - 32;
            if (game.LastPlayed != "" && this.Width>550)
                DrawInfoBox(g, $"{ScePSX.Properties.Resources.RomList_DrawInfoBoxes_最后运行}: {game.LastPlayed}", startX - 29, startY, 9);
            DrawInfoBox(g, $"{ScePSX.Properties.Resources.RomList_DrawInfoBoxes_即时存档}: {(game.HasSaveState ? "✓" : "✗")}", startX + 166, startY, 9);
            DrawInfoBox(g, $"{ScePSX.Properties.Resources.RomList_DrawInfoBoxes_金手指}: {(game.HasCheats ? "✓" : "✗")}", startX + 260, startY, 9);
        }

        private void DrawSelectionEffect(Graphics g, Rectangle bounds)
        {
            using (var focusPen = new Pen(SelectionColor, 2))
            {
                g.DrawRectangle(focusPen, bounds.X + 1, bounds.Y + 1, bounds.Width - 3, bounds.Height - 3);
            }
        }

        private void DrawInfoBox(Graphics g, string label, int x, int y, int fontSize = 9)
        {
            using (var boxBrush = new SolidBrush(InfoBackColor)) // 框背景颜色
            using (var borderPen = new Pen(InfoBorderColor)) // 边框颜色
            using (var textBrush = new SolidBrush(Color.White)) // 文字颜色
            using (var font = new Font("Arial", fontSize))
            {
                int padding = 4;

                SizeF labelSize = g.MeasureString(label, font);

                g.FillRectangle(boxBrush, x, y, labelSize.Width + padding * 2, labelSize.Height + padding * 2);
                g.DrawRectangle(borderPen, x, y, labelSize.Width + padding * 2, labelSize.Height + padding * 2);

                g.DrawString(label, font, textBrush, x + padding, y + padding);
            }
        }

        private void DrawInfoBoxValue(Graphics g, string label, string value, int x, int y, int width, int height, int fontsize = 9)
        {
            using (var boxBrush = new SolidBrush(Color.FromArgb(50, 50, 50))) // 框背景颜色
            using (var borderPen = new Pen(Color.FromArgb(100, 100, 100))) // 边框颜色
            using (var textBrush = new SolidBrush(Color.White)) // 文字颜色
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

        #region MENU
        public class CustomToolStripRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected)
                {
                    using (var brush = new SolidBrush(RomList.MenuSelectColor))
                    {
                        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                    }
                } else
                {
                    using (var brush = new SolidBrush(RomList.MenuUnSelectColor))
                    {
                        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                    }
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = Color.White;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                using (var pen = new Pen(RomList.SepColor))
                {
                    e.Graphics.DrawLine(pen, e.Item.ContentRectangle.Left, e.Item.ContentRectangle.Height / 2, e.Item.ContentRectangle.Right, e.Item.ContentRectangle.Height / 2);
                }
            }

            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
            {
                Rectangle imageMarginBounds = new Rectangle(
                    e.AffectedBounds.Left,
                    e.AffectedBounds.Top,
                    e.AffectedBounds.Width,
                    e.AffectedBounds.Height
                );

                using (var brush = new SolidBrush(RomList.MenuUnSelectColor))
                {
                    e.Graphics.FillRectangle(brush, imageMarginBounds);
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (var pen = new Pen(RomList.SepColor))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                }
            }
        }
        #endregion

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            e.ItemHeight = ItemHeight;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            scrollBarBounds = new Rectangle(
                ClientRectangle.Width - ScrollBarWidth - ScrollBarMargin,
                ScrollBarMargin,
                ScrollBarWidth,
                Math.Max(ThumbMinSize * 2, ClientRectangle.Height - 2 * ScrollBarMargin)
            );

            UpdateScrollBar();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            int delta = e.Delta * SystemInformation.MouseWheelScrollLines / 120 * ItemHeight;
            thumbPosition -= delta;

            thumbPosition = Math.Max(0, Math.Min(thumbPosition, scrollBarBounds.Height - thumbSize));

            UpdateScrollPosition();
            Invalidate();
        }

        public new Rectangle GetItemRectangle(int index)
        {
            Rectangle baseRect = base.GetItemRectangle(index);
            // 动态计算内容宽度
            int contentWidth = _isScrollBarVisible ?
                ClientSize.Width - ScrollBarWidth - ScrollBarMargin * 2 :
                ClientSize.Width;
            baseRect.Width = contentWidth;
            return baseRect;
        }

        public int myIndexFromPoint(Point p)
        {
            if (p.X >= ClientSize.Width - scrollBarBounds.Width)
                return -1; // 点击在滚动条区域

            Point adjustedPoint = new Point(
                Math.Min(p.X, ClientSize.Width - scrollBarBounds.Width - 1),
                p.Y
            );

            return base.IndexFromPoint(adjustedPoint);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            int firstVisibleIndex = TopIndex;
            int itemsPerPage = ClientSize.Height / ItemHeight;
            int lastVisibleIndex = Math.Min(Items.Count - 1, firstVisibleIndex + itemsPerPage + 1);

            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                Rectangle itemRect = GetItemRectangle(i);
                if (itemRect.Bottom < 0 || itemRect.Top > ClientSize.Height)
                    continue;

                DrawItemState state = DrawItemState.Default;
                if (i == SelectedIndex)
                    state |= DrawItemState.Selected;

                DrawItemEventArgs args = new DrawItemEventArgs(
                    e.Graphics,
                    Font,
                    itemRect,
                    i,
                    state
                );
                OnDrawItem(args);
            }

            DrawScrollBar(g);
        }

        private void DrawScrollBar(Graphics g)
        {
            if (!_isScrollBarVisible)
                return;

            // 检查滚动条轨道有效性
            if (scrollBarBounds.Width <= 0 || scrollBarBounds.Height <= 0)
                return;

            // 绘制轨道背景
            using (var trackBrush = new SolidBrush(TrackColor))
            using (var borderPen = new Pen(Color.FromArgb(80, 80, 80)))
            {
                g.FillRectangle(trackBrush, scrollBarBounds);
                g.DrawRectangle(borderPen, scrollBarBounds);
            }

            // 初始化滑块区域
            thumbBounds = new Rectangle(
                scrollBarBounds.X + 2,
                scrollBarBounds.Y + thumbPosition,
                scrollBarBounds.Width - 4,
                Math.Max(1, thumbSize) // 确保高度至少为1像素
            );

            // 绘制滑块
            bool isHovered = thumbBounds.Contains(PointToClient(Cursor.Position));
            using (var thumbBrush = new LinearGradientBrush(
                thumbBounds,
                isHovered ? ThumbHoverColor : ThumbColor,
                Color.FromArgb(isHovered ? 80 : 60, 80, 80),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(thumbBrush, thumbBounds);
                using (var highlightPen = new Pen(Color.FromArgb(150, 150, 150)))
                {
                    g.DrawLine(highlightPen, thumbBounds.Left + 1, thumbBounds.Top + 1,
                        thumbBounds.Right - 2, thumbBounds.Top + 1);
                }
            }

            // 绘制滑块边框
            using (var thumbBorderPen = new Pen(Color.FromArgb(180, 180, 180)))
            {
                g.DrawRectangle(thumbBorderPen, thumbBounds);
            }
        }

        private void UpdateScrollBar()
        {

            _isScrollBarVisible = Items.Count * ItemHeight > ClientSize.Height;

            if (!_isScrollBarVisible)
            {
                thumbSize = 0;
                thumbPosition = 0;
                return;
            }

            if (scrollBarBounds.Height <= 0)
            {
                scrollBarBounds.Height = Math.Max(ThumbMinSize * 2, ClientSize.Height);
            }

            int visibleItems = ClientSize.Height / ItemHeight;
            int totalItems = Items.Count;

            thumbSize = Math.Max(
                ThumbMinSize,
                (int)((visibleItems / (float)totalItems) * scrollBarBounds.Height)
            );
            thumbSize = Math.Min(thumbSize, scrollBarBounds.Height);

            int maxTopIndex = Math.Max(0, totalItems - visibleItems);
            if (maxTopIndex == 0)
            {
                thumbPosition = 0;
            } else
            {
                thumbPosition = (int)((TopIndex / (float)maxTopIndex) * (scrollBarBounds.Height - thumbSize));
            }

            thumbPosition = Math.Max(0, Math.Min(thumbPosition, scrollBarBounds.Height - thumbSize));
        }

        private void UpdateScrollPosition()
        {
            if (Items.Count == 0)
                return;

            int visibleItems = ClientSize.Height / ItemHeight;
            int totalItems = Items.Count;
            int maxTopIndex = Math.Max(0, totalItems - visibleItems);

            if (scrollBarBounds.Height - thumbSize != 0)
            {
                float ratio = thumbPosition / (float)(scrollBarBounds.Height - thumbSize);
                TopIndex = (int)(ratio * maxTopIndex);
            }

            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DefaultIcon.Dispose();
                foreach (var item in Items)
                {
                    if((item as Game).Icon != null)
                        (item as Game).Icon.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {
           
            base.WndProc(ref m);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.Style &= ~0x00100000; // WS_HSCROLL (水平滚动条)
                createParams.Style &= ~0x00200000; // WS_VSCROLL (垂直滚动条)
                return (createParams);
            }
        }
    }

}
