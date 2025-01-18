﻿using System;
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
        FileVersionInfo fileInfo = null;

        public EntryInteractModal(BamEntry entry)
        {
            InitializeComponent();

            // saves entry
            this.entry = entry;
            this.exeName = this.entry.Name.Split('\\')[this.entry.Name.Split('\\').Count() - 1];

            this.txtInput.Text = this.entry.Name;

            /*
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
            */

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
                        WindowIcon.Source = bitmapSource;
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
