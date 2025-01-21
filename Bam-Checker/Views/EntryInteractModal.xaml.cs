using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BamChecker.BAM;
using BamChecker.UI;
using static BamChecker.DllImports;

namespace BamChecker.Views
{
    public partial class EntryInteractModal : Window
    {
        BamEntry entry { set; get; }
        string exeName = "";
        string folderPath = "";
        
        readonly FileVersionInfo fileInfo;

        public EntryInteractModal(BamEntry entry)
        {
            InitializeComponent();

            // saves entry
            this.entry = entry;
            this.exeName = Path.GetFileName(this.entry.Name);

            this.txtInput.Text = this.entry.Name;
            this.bamInput.Text = this.entry.BAM_Path;

            this.folderPath = string.Join("\\", this.entry.Name.Split('\\').Take(this.entry.Name.Split('\\').Length - 1).ToArray());
            this.folderInput.Text = this.folderPath;
            this.nameInput.Text = this.exeName;

            this.pfInput.Text = this.entry.Pf_File_Path;
            if (File.Exists(this.entry.Pf_File_Path)) this.modificationText.Text = $"Last Modification Time: {File.GetLastWriteTime(this.entry.Pf_File_Path)}";
            else this.modificationText.Visibility = Visibility.Collapsed;

            // if exist
            if (File.Exists(entry.Name))
            {
                // more informations
                this.fileInfo = FileVersionInfo.GetVersionInfo(entry.Name);


                this.versionText.Text = $"Version: {this.fileInfo.ProductVersion}";
                this.moreInfoText.Text = $"{this.fileInfo.ProductName}, {this.fileInfo.CompanyName}, {this.fileInfo.FileDescription}, {this.fileInfo.LegalCopyright}";

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
                            BitmapSizeOptions.FromEmptyOptions()
                        );

                        FileIcon.Source = bitmapSource;
                        WindowIcon.Source = bitmapSource;
                    }
                    finally
                    {
                        DllImports.DeleteObject(shinfo.hIcon);
                        DllImports.DestroyIcon(shinfo.hIcon);
                    }
                }
            }
            else
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/Views/Assets/Missing_File.png");
                bitmap.EndInit();

                FileIcon.Source = bitmap;
                WindowIcon.Source = bitmap;
            }

            // title
            this.Title = $"{this.exeName} - {this.entry.Session_Text} | {this.entry.Signature}";
        }

        private void Open_File_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(this.entry.Name))
            {
                Pages.Error("This folder cannot be opened.", false);
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{entry.Name}\"");
        }
        private void Copy_File_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.entry.Name);
        }

        private void Open_BAM_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(this.entry.Name))
            {
                Pages.Error("This folder cannot be opened.", false);
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{entry.Name}\"");
        }
        private void Copy_BAM_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.entry.BAM_Path);
        }

        private void Open_Folder_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(this.folderPath))
            {
                Pages.Error("This folder cannot be opened.", false);
                return;
            }

            Process.Start("explorer.exe", this.folderPath);
        }
        private void Copy_Folder_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.folderPath);
        }

        private void Copy_Name_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.exeName);
        }

        private void Open_Prefetch_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(this.entry.Pf_File_Path))
            {
                Pages.Error("This folder cannot be opened.", false);
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{this.entry.Pf_File_Path}\"");
        }
        private void Copy_Prefetch_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.entry.Pf_File_Path);
        }

        private void Inspect_Imports_Click(object sender, RoutedEventArgs e)
        {
            InspectImportsModal modal = new InspectImportsModal(this.entry, WindowIcon.Source);
            modal.Show();
        }

        // title bar func
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (WindowStyle != WindowStyle.None)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (DispatcherOperationCallback)delegate (object unused)
                {
                    WindowStyle = WindowStyle.None;
                    return null;
                }
                , null);
            }
        }

        void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            SystemCommands.MinimizeWindow(this);
        }

        void MaximizeRestoreWindow(object sender, RoutedEventArgs e)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            if (this.WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        void CloseWindow(object sender, RoutedEventArgs e)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            this.Close();
        }

        void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowState = WindowState.Normal;
            }

            if (e.ClickCount == 2)
            {
                ToggleWindowState();
            }
            else
            {
                DragMove();
            }
        }

        void ToggleWindowState()
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }
    }
}
