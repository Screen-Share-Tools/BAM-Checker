using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BamChecker.BAM;
using BamChecker.UI;
using static BamChecker.DllImports;

namespace BamChecker.Views
{
    public partial class EntryInteractModal : Window
    {
        BamEntry entry { set; get; }
        string exeName = "";
        FileVersionInfo fileInfo = null;

        public EntryInteractModal(BamEntry entry)
        {
            InitializeComponent();

            // saves entry
            this.entry = entry;
            this.exeName = this.entry.Name.Split('\\')[this.entry.Name.Split('\\').Count() - 1];

            this.txtInput.Text = this.entry.Name;

            if (this.entry.Is_In_Session)
            {
                sessionText.Text = this.entry.Session_Text;
                sessionText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#28a745"));
            }
            else
            {
                sessionText.Text = this.entry.Session_Text;
                sessionText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#dc3545"));
            }

            // if exist
            if (File.Exists(entry.Name))
            {
                // more informations
                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(entry.Name);

                this.versionText.Text = $"Version: {fileInfo.ProductVersion}";
                this.moreInfoText.Text = $"{fileInfo.ProductName}, {fileInfo.CompanyName}, {fileInfo.FileDescription}, {fileInfo.LegalCopyright}";

                // icon
                SHFILEINFO shinfo = new SHFILEINFO();
                DllImports.SHGetFileInfo(entry.Name, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), DllImports.SHGFI_ICON | DllImports.SHGFI_LARGEICON);

                using (System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(shinfo.hIcon))
                {
                    try
                    {
                        BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                        FileIcon.Source = bitmapSource;
                    }
                    finally
                    {
                        DllImports.DeleteObject(shinfo.hIcon);
                        DllImports.DestroyIcon(shinfo.hIcon);
                    }
                }
            }

            // title
            this.Title = $"{this.exeName} - {this.entry.Session_Text}";
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(this.entry.Name))
            {
                Pages.Error("This folder cannot be opened.", false);
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{entry.Name}\"");
        }
    }
}
