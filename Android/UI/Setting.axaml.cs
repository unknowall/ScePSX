using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

#pragma warning disable CS8604
#pragma warning disable CS8629
#pragma warning disable CS8618

namespace ScePSX;

public partial class Setting : UserControl
{
    private string Title;
    private string id;
    private IniFile ini;

    public Setting()
    {
        InitializeComponent();
    }

    public Setting(string gameid) : this()
    {
        id = gameid;

        cbcdrom.Items[0] = Translations.GetText("Adaptive");
        cbgpures.Items[0] = Translations.GetText("Adaptive");
        cbgpu.Items[0] = Translations.GetText("Adaptive");

        cbcpumode.Items.Add(Translations.GetText("cpuPerf"));
        cbcpumode.Items.Add(Translations.GetText("cpuNormal"));

        if (id == "")
        {
            loadini(PSXHandler.ini);
            btndel.IsVisible = false;
        } else
        {
            string fn = AHelper.RootPath + $"/Save/{id}.ini";
            ini = new IniFile(AHelper.RootPath + $"/Save/{id}.ini");
            if (!File.Exists(fn))
            {
                loadini(PSXHandler.ini);
            } else
            {
                loadini(ini);
            }
            btndel.IsVisible = true;
            labGameID.IsVisible = true;
            labGameID.Text = $"🎮 {id}";
        }

        cbgpures.IsEditable = true;
        cbgpu.SelectionChanged += Cbgpu_SelectionChanged;

        BtnSave.Click += BtnSave_Click;
        btndel.Click += Btndel_Click;
        btnbios.Click += Btnbios_Click;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        Title = Translations.GetText("FrmSetting", "forms");
        this.Title = $" {id} " + this.Title;
    }

    private async void Btnbios_Click(object? sender, RoutedEventArgs e)
    {
        string result = await AHelper.SelectFile("BIOS", "", true, AHelper.CommonActivity);
        if (File.Exists(result))
        {
            FileInfo fileInfo = new FileInfo(result);
            if (fileInfo.Length != 524288)
            {
                OSD.Show(Translations.GetText("invbios"));
                return;
            }
            PSXHandler.ini.Write("main", "bios", result);
        }
    }

    private void BtnSave_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (id == "")
        {
            saveini(PSXHandler.ini);
        } else
        {
            saveini(ini);
        }

        applypgxp();
    }

    private void Btndel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (id == "")
            return;

        string fn = AHelper.RootPath + $"/Save/{id}.ini";
        if (File.Exists(fn))
        {
            File.Delete(fn);
        }
        loadini(PSXHandler.ini);

        AHelper.CommonActivity?.Finish();
    }

    private void Cbgpu_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
    {
        if (cbgpu.SelectedIndex != 1)
        {
            cbgpures.IsEnabled = true;
            chkpgxpt.IsEnabled = true;
            chkkeepar.IsEnabled = true;
        } else
        {
            cbgpures.IsEnabled = false;
            chkpgxpt.IsEnabled = false;
            chkkeepar.IsEnabled = false;
        }
    }

    private void applypgxp()
    {
        ScePSX.Core.GPU.PGXPVector.use_pgxp = chkpgxp.IsChecked ?? false;
        ScePSX.Core.GPU.PGXPVector.use_pgxp_avs = chkpgxp_avs.IsChecked ?? false;
        ScePSX.Core.GPU.PGXPVector.use_pgxp_clip = chkpgxp_clip.IsChecked ?? false;
        ScePSX.Core.GPU.PGXPVector.use_pgxp_aff = chkpgxp_aff.IsChecked ?? false;
        ScePSX.Core.GPU.PGXPVector.use_pgxp_nc = chkpgxp_nc.IsChecked ?? false;
        ScePSX.Core.GPU.PGXPVector.use_pgxp_highpos = chkpgxp_highpos.IsChecked ?? false;
        ScePSX.Core.GPU.PGXPVector.use_pgxp_memcap = chkpgxp_memcap.IsChecked ?? false;
        ScePSX.Core.GPU.PGXPVector.use_perspective_correction = chkpgxp_ppc.IsChecked ?? false;
    }

    private void loadini(IniFile ini)
    {
        tbbuscycles.Text = ini.Read("CPU", "BusCycles");
        tbcylesfix.Text = ini.Read("CPU", "CyclesFix");
        tbframeidle.Text = ini.Read("CPU", "FrameIdle");
        tbframeskip.Text = ini.Read("Main", "SkipFrame");
        tbcputicks.Text = ini.Read("CPU", "CpuTicks");
        tbaudiobuffer.Text = ini.Read("Audio", "Buffer");

        cbmsaa.SelectedIndex = ini.ReadInt("OpenGL", "MSAA");

        cbscalemode.SelectedIndex = ini.ReadInt("Main", "ScaleMode");

        //chkbios.IsChecked = ini.ReadInt("Main", "BiosDebug") == 1;
        //chkcpu.IsChecked = ini.ReadInt("Main", "CPUDebug") == 1;
        //chkTTY.IsChecked = ini.ReadInt("Main", "TTYDebug") == 1;

        chkpgxpt.IsChecked = ini.ReadInt("Main", "PGXPT") == 1;
        chkrealcolor.IsChecked = ini.ReadInt("Main", "RealColor") == 1;
        chkkeepar.IsChecked = ini.ReadInt("Main", "KeepAR") == 1;

        cbcpumode.SelectedIndex = ini.ReadInt("Main", "CpuMode");

        cbgpu.SelectedIndex = ini.ReadInt("Main", "GpuMode");

        cbgpures.SelectedIndex = ini.ReadInt("Main", "GpuModeScale");

        cbcdrom.SelectedIndex = ini.ReadInt("Main", "CdSpeed");

        ChkFMV.IsChecked = ini.ReadInt("Main", "24bitfmv") == 1;

        chkpgxp.IsChecked = ini.ReadInt("PGXP", "base") == 1;
        chkpgxp_aff.IsChecked = ini.ReadInt("PGXP", "aff") == 1;
        chkpgxp_avs.IsChecked = ini.ReadInt("PGXP", "avs") == 1;
        chkpgxp_clip.IsChecked = ini.ReadInt("PGXP", "clip") == 1;
        chkpgxp_nc.IsChecked = ini.ReadInt("PGXP", "nc") == 1;
        chkpgxp_highpos.IsChecked = ini.ReadInt("PGXP", "highpos") == 1;
        chkpgxp_memcap.IsChecked = ini.ReadInt("PGXP", "memcap") == 1;
        chkpgxp_ppc.IsChecked = ini.ReadInt("PGXP", "ppc") == 1;

        var currbios = ini.Read("main", "bios");
        //DirectoryInfo dir = new DirectoryInfo(AHelper.RootPath + "/BIOS");
        //if (dir.Exists)
        //{
        //    if (dir.GetFiles().Length == 0)
        //    {
        //        return;
        //    }
        //    cbbios.Items.Clear();
        //    foreach (FileInfo f in dir.GetFiles())
        //    {
        //        cbbios.Items.Add(f.Name);
        //        if (currbios == f.Name)
        //            cbbios.SelectedIndex = cbbios.Items.Count - 1;
        //    }
        //}
        if (File.Exists(currbios))
        {
            edbios.Text = Path.GetFileName(currbios);
        }

        Cbgpu_SelectionChanged(null, null);
    }

    private void saveini(IniFile ini)
    {
        try
        {
            ini.WriteInt("CPU", "BusCycles", int.Parse(tbbuscycles.Text));
            ini.WriteInt("CPU", "CyclesFix", int.Parse(tbcylesfix.Text));
            ini.WriteFloat("CPU", "FrameIdle", double.Parse(tbframeidle.Text));
            ini.WriteInt("Main", "SkipFrame", int.Parse(tbframeskip.Text));
            ini.WriteInt("CPU", "CpuTicks", int.Parse(tbcputicks.Text));
            ini.WriteInt("Audio", "Buffer", int.Parse(tbaudiobuffer.Text));

            ini.WriteInt("OpenGL", "MSAA", cbmsaa.SelectedIndex);

            //ini.WriteInt("Main", "BiosDebug", chkbios.IsChecked ?? false ? 1 : 0);
            //ini.WriteInt("Main", "CPUDebug", chkcpu.IsChecked ?? false ? 1 : 0);
            //ini.WriteInt("Main", "TTYDebug", chkTTY.IsChecked ?? false ? 1 : 0);

            ini.WriteInt("Main", "PGXP", chkpgxp.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("Main", "PGXPT", chkpgxpt.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("Main", "RealColor", chkrealcolor.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("Main", "KeepAR", chkkeepar.IsChecked ?? false ? 1 : 0);

            ini.WriteInt("Main", "CpuMode", cbcpumode.SelectedIndex);

            ini.WriteInt("Main", "GpuMode", cbgpu.SelectedIndex);

            ini.WriteInt("Main", "GpuModeScale", cbgpures.SelectedIndex);

            ini.WriteInt("Main", "ScaleMode", cbscalemode.SelectedIndex);

            ini.WriteInt("Main", "CdSpeed", cbcdrom.SelectedIndex);

            ini.WriteInt("Main", "24bitfmv", ChkFMV.IsChecked ?? false ? 1 : 0);

            //ini.Write("main", "bios", cbbios.Items[cbbios.SelectedIndex].ToString());

            ini.WriteInt("PGXP", "base", chkpgxp.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("PGXP", "aff", chkpgxp_aff.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("PGXP", "avs", chkpgxp_avs.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("PGXP", "clip", chkpgxp_clip.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("PGXP", "highpos", chkpgxp_highpos.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("PGXP", "memcap", chkpgxp_memcap.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("PGXP", "nc", chkpgxp_nc.IsChecked ?? false ? 1 : 0);
            ini.WriteInt("PGXP", "ppc", chkpgxp_ppc.IsChecked ?? false ? 1 : 0);

        } catch
        {
        }
    }
}
