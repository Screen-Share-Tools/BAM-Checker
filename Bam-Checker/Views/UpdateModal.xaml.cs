using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BamChecker.UI;

namespace BamChecker.Views
{
    public partial class UpdateModal : Window
    {
        string downloadUrl {  get; set; }
        string latestVersion { get; set; }

        public UpdateModal(string downloadUrl, string latestVersion)
        {
            InitializeComponent();

            this.downloadUrl = downloadUrl;
            this.latestVersion = latestVersion;

            this.VersionText.Text = $"Version: {latestVersion}";

            // icon
            using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location))
            {
                try
                {
                    BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                    WindowIcon.Source = bitmapSource;
                }
                finally
                {
                    DllImports.DeleteObject(icon.Handle);
                    DllImports.DestroyIcon(icon.Handle);
                }
            }

        }

        // actions
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            this.Close();
        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "new_version.exe");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] data = await client.GetByteArrayAsync(downloadUrl);
                    File.WriteAllBytes(tempFile, data);
                }

                int currentProcessId = Process.GetCurrentProcess().Id;
                string batchScript = $@"
                    @echo off
                    taskkill /PID {currentProcessId} /F >nul 2>&1
                    timeout 1
                    move /y ""{tempFile}"" ""{Process.GetCurrentProcess().MainModule.FileName}""
                    start """" ""{Process.GetCurrentProcess().MainModule.FileName}""
                    del ""%~f0"" & exit
                ";

                string batchFile = Path.Combine(Path.GetTempPath(), "bam_checker_updater.bat");
                File.WriteAllText(batchFile, batchScript);

                // Avvia il file batch
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchFile,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Pages.Error($"Update error: {ex.Message}");
            }
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

            DragMove();
        }
    }
}
