using System;
using Android.Content;
using Avalonia.Controls;
using Avalonia.Input;

#pragma warning disable CS8600

namespace ScePSX
{
    public partial class AboutFrm : UserControl
    {
        public AboutFrm()
        {
            InitializeComponent();

            labver.Text = MainView.Version;

            labinfo.Text = Translations.GetText("FrmAbout_memo1");

            labSupport.Text = Translations.GetText("support_message");
        }

        private void Link_LinkClicked(object sender, PointerPressedEventArgs e)
        {
            OpenUrl("https://github.com/unknowall/ScePSX");
        }

        private void SupportLink_Click(object sender, PointerPressedEventArgs e)
        {
            OpenUrl("https://ko-fi.com/unknowall");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Android.Net.Uri uri = Android.Net.Uri.Parse(url);
                Intent intent = new Intent(Intent.ActionView, uri);
                intent.AddFlags(ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);

            } catch (Exception ex)
            {
                Console.WriteLine($"Failed to open URL: {ex.Message}");
            }
        }
    }
}
