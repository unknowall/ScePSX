using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using static ScePSX.Controller;

namespace ScePSX.UI;

public partial class KeyConfig : Window
{
    KeyMange KeyM;

    private InputAction SetKey;
    private Button Btn;

    public KeyConfig(KeyMange KeySet)
    {
        InitializeComponent();

        KeyM = KeySet;

        KeyM.InitKeyMap();

        cbcon.SelectionChanged += Cbcon_SelectionChanged;

        KeyDown += FrmInput_KeyDown;

        L1.Click += L1_Click;
        L2.Click += L2_Click;
        R1.Click += R1_Click;
        R2.Click += R2_Click;
        U.Click += U_Click;
        D.Click += D_Click;
        L.Click += L_Click;
        R.Click += R_Click;
        TRI.Click += TRI_Click;
        SQUAD.Click += SQUAD_Click;
        O.Click += O_Click;
        X.Click += X_Click;
        SELE.Click += SELE_Click;
        START.Click += START_Click;

        BtnSave.Click += BtnSave_Click;
        BtnReset.Click += BtnReset_Click;

        UpdateButtonTexts();
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        KeyM.SaveKeyMap();

        Close();
    }

    private void BtnReset_Click(object? sender, RoutedEventArgs e)
    {
        KeyM.InitKeyMap();
        UpdateButtonTexts();
    }

    private void FrmInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            plwait.IsVisible = false;
            return;
        }

        if (!plwait.IsVisible)
            return;

        Btn.Content = e.Key.ToString().ToUpper();

        var kmm = GetCurrentKeyMappingManager();
        kmm.SetKeyMapping(e.Key, SetKey);

        plwait.IsVisible = false;
        UpdateButtonTexts();
    }

    private void ReadyGetKey(object? sender, InputAction val)
    {
        if (sender == null)
            return;

        plwait.IsVisible = true;
        Btn = (Button)sender;
        SetKey = val;
    }

    private void L1_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.L1);
    private void L2_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.L2);
    private void R1_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.R1);
    private void R2_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.R2);
    private void U_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.DPadUp);
    private void D_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.DPadDown);
    private void L_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.DPadLeft);
    private void R_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.DPadRight);
    private void TRI_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.Triangle);
    private void SQUAD_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.Square);
    private void O_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.Circle);
    private void X_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.Cross);
    private void SELE_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.Select);
    private void START_Click(object? sender, RoutedEventArgs e) => ReadyGetKey(sender, InputAction.Start);

    private void Cbcon_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateButtonTexts();
    }

    private KeyMappingManager GetCurrentKeyMappingManager()
    {
        return cbcon.SelectedIndex == 0 ? KeyMange.KMM1 : KeyMange.KMM2;
    }

    private void UpdateButtonTexts()
    {
        var kmm = GetCurrentKeyMappingManager();

        START.Content = kmm.GetKeyCode(InputAction.Start).ToString().ToUpper();
        SELE.Content = kmm.GetKeyCode(InputAction.Select).ToString().ToUpper();
        U.Content = kmm.GetKeyCode(InputAction.DPadUp).ToString().ToUpper();
        D.Content = kmm.GetKeyCode(InputAction.DPadDown).ToString().ToUpper();
        L.Content = kmm.GetKeyCode(InputAction.DPadLeft).ToString().ToUpper();
        R.Content = kmm.GetKeyCode(InputAction.DPadRight).ToString().ToUpper();
        L1.Content = kmm.GetKeyCode(InputAction.L1).ToString().ToUpper();
        R1.Content = kmm.GetKeyCode(InputAction.R1).ToString().ToUpper();
        L2.Content = kmm.GetKeyCode(InputAction.L2).ToString().ToUpper();
        R2.Content = kmm.GetKeyCode(InputAction.R2).ToString().ToUpper();
        X.Content = kmm.GetKeyCode(InputAction.Cross).ToString().ToUpper();
        O.Content = kmm.GetKeyCode(InputAction.Circle).ToString().ToUpper();
        TRI.Content = kmm.GetKeyCode(InputAction.Triangle).ToString().ToUpper();
        SQUAD.Content = kmm.GetKeyCode(InputAction.Square).ToString().ToUpper();
    }
}
