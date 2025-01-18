using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BamChecker.BAM;
using BamChecker.UI;
using BamChecker.Views;
using System.Windows.Threading;
using System.Threading;

namespace BamChecker
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<BamEntry> BamEntries { get; set; }
        public List<BamEntry> allEntries { get; set; }

        Pages pages;
        bool firstCheck = true;

        public static DateTime sessionDate = DateTime.Now;

        public MainWindow()
        {
            // init
            InitializeComponent();

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


            // bam init
            this.allEntries = new List<BamEntry>();
            this.BamEntries = new ObservableCollection<BamEntry>();
            this.DataContext = this;

            // pages
            this.pages = new Pages(this);
            this.pages.Add("firstPage", this.firstPage);
            this.pages.Add("secondPage", this.secondPage);
            this.pages.Add("thirdPage", this.thirdPage);
        }

        // actions
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.pages.Hide("firstPage");
            this.pages.Show("secondPage");

            // session date
            if (firstCheck)
            {
                sessionDate = await GetSystemBootTime();
                firstCheck = false;
            }

            // parse BAM
            List<BamEntry> tempEntries = new List<BamEntry>();
            await Task.Run(() =>
            {
                BamEntry[] entries = BAM.BAM.getBamEntries();
                foreach (BamEntry entry in entries)
                {
                    tempEntries.Add(entry);
                }
            });


            foreach (var entry in tempEntries)
            {
                this.BamEntries.Add(entry);
                this.allEntries.Add(entry);
            }

            // automatic filter
            var collectionView = CollectionViewSource.GetDefaultView(BamEntries);
            collectionView.SortDescriptions.Clear();
            collectionView.SortDescriptions.Add(new SortDescription("UTC_Time", ListSortDirection.Descending));
            this.timeCol.SortDirection = ListSortDirection.Descending;

            this.pages.Hide("secondPage");
            this.pages.Show("thirdPage");
        }

        private async void Fetch_Again_Click(object sender, RoutedEventArgs e)
        {
            this.BamEntries.Clear();
            this.allEntries.Clear();

            this.pages.Hide("thirdPage");
            this.pages.Show("secondPage");

            // session date
            if (firstCheck)
            {
                sessionDate = await GetSystemBootTime();
                firstCheck = false;
            }

            // parse BAM
            List<BamEntry> tempEntries = new List<BamEntry>();
            await Task.Run(() =>
            {
                BamEntry[] entries = BAM.BAM.getBamEntries();
                foreach (BamEntry entry in entries)
                {
                    tempEntries.Add(entry);
                }
            });

            foreach (var entry in tempEntries)
            {
                this.BamEntries.Add(entry);
                this.allEntries.Add(entry);
            }

            // automatic filter
            var collectionView = CollectionViewSource.GetDefaultView(BamEntries);
            collectionView.SortDescriptions.Clear();
            collectionView.SortDescriptions.Add(new SortDescription("UTC_Time", ListSortDirection.Descending));
            this.timeCol.SortDirection = ListSortDirection.Descending;

            this.pages.Hide("secondPage");
            this.pages.Show("thirdPage");
        }

        private void DataGridCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGridCellTarget = (DataGridCell)sender;
            var row = DataGridRow.GetRowContainingElement(dataGridCellTarget);
            if (row == null) return;

            var item = row.Item;

            string name = (string)item.GetType().GetProperty("Name")?.GetValue(item);
            BamEntry entry = this.BamEntries.ToList().Find(x => x.Name == name);
            if (entry == null) return;

            var entryInteractModal = new EntryInteractModal(entry);
            entryInteractModal.Show();
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = GetScrollViewer(this);
            if (scrollViewer != null)
            {
                if (e.Delta > 0)
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 30);
                else
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 30);

                e.Handled = true;
            }
        }
        private ScrollViewer GetScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                var result = GetScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
        private void BamDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.Properties_Executed();
            }
            else if (e.Key == Key.Delete)
            {
                e.Handled = true;
                this.Hide_Executed();
            }
        }

        // header func
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
            Application.Current.Shutdown();
        }

        void DragWindow(object sender, MouseButtonEventArgs e)
        {
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

        // context menu
        private void PropertiesCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = bamGrid.SelectedCells.Count > 0;
        }

        private void PropertiesCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Properties_Executed();
        }

        private void Properties_Executed()
        {
            if (bamGrid.SelectedCells.Count > 0)
            {
                var cells = bamGrid.SelectedCells
                    .GroupBy(cell => cell.Item)
                    .Select(group => group.First())
                    .ToList();

                foreach (var selectedCell in cells)
                {
                    var rowItem = selectedCell.Item;

                    var nameValue = rowItem.GetType().GetProperty("Name")?.GetValue(rowItem, null);
                    if (string.IsNullOrEmpty((string)nameValue)) return;

                    BamEntry entry = this.BamEntries.ToList().Find(x => x.Name == (string)nameValue);
                    if (entry == null) return;

                    var entryInteractModal = new EntryInteractModal(entry);
                    entryInteractModal.Show();
                }
            }
        }

        private void Open_In_Explorer(object sender, RoutedEventArgs e)
        {
            if (bamGrid.SelectedCells.Count > 0)
            {
                var cells = bamGrid.SelectedCells
                    .GroupBy(cell => cell.Item)
                    .Select(group => group.First())
                    .ToList();

                foreach (var selectedCell in cells)
                {
                    var rowItem = selectedCell.Item;

                    var nameValue = rowItem.GetType().GetProperty("Name")?.GetValue(rowItem, null);
                    if (string.IsNullOrEmpty((string)nameValue)) return;

                    BamEntry entry = this.BamEntries.ToList().Find(x => x.Name == (string)nameValue);
                    if (entry == null) return;

                    if (!File.Exists(entry.Name))
                    {
                        Pages.Error("This folder cannot be opened.", false);
                        return;
                    }

                    Process.Start("explorer.exe", $"/select,\"{entry.Name}\"");
                }
            }
        }

        private void Open_File(object sender, RoutedEventArgs e)
        {
            if (bamGrid.SelectedCells.Count > 0)
            {
                var cells = bamGrid.SelectedCells
                    .GroupBy(cell => cell.Item)
                    .Select(group => group.First())
                    .ToList();

                foreach (var selectedCell in cells)
                {
                    var rowItem = selectedCell.Item;

                    var nameValue = rowItem.GetType().GetProperty("Name")?.GetValue(rowItem, null);
                    if (string.IsNullOrEmpty((string)nameValue)) return;

                    BamEntry entry = this.BamEntries.ToList().Find(x => x.Name == (string)nameValue);
                    if (entry == null) return;

                    if (!File.Exists(entry.Name))
                    {
                        Pages.Error("This file cannot be opened.", false);
                        return;
                    }

                    Process.Start(entry.Name);
                }
            }
        }

        private void HideCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = bamGrid.SelectedCells.Count > 0;
        }

        private void HideCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Hide_Executed();
        }

        private void Hide_Executed()
        {
            if (bamGrid.SelectedCells.Count > 0)
            {
                var cells = bamGrid.SelectedCells
                    .GroupBy(cell => cell.Item)
                    .Select(group => group.First())
                    .ToList();

                foreach (var selectedCell in cells)
                {
                    var rowItem = selectedCell.Item;

                    var nameValue = rowItem.GetType().GetProperty("Name")?.GetValue(rowItem, null);
                    if (string.IsNullOrEmpty((string)nameValue)) return;

                    BamEntry entry = this.BamEntries.ToList().Find(x => x.Name == (string)nameValue);
                    if (entry == null) return;

                    this.BamEntries.Remove(entry);
                }
            }
        }

        // static
        private CancellationTokenSource cancellationTokenSource;
        static public async Task<DateTime> GetSystemBootTime()
        {
            try
            {
                return await Task.Run(() =>
                {
                    var query = "*[System/EventID=4624 and (EventData/Data[@Name='LogonType']='2' or EventData/Data[@Name='LogonType']='3' or EventData/Data[@Name='LogonType']='11')]";
                    EventLogQuery eventLogQuery = new EventLogQuery("Security", PathType.LogName, query);
                    EventLogReader reader = new EventLogReader(eventLogQuery);

                    var logonTimes = new List<DateTime>();

                    EventRecord eventRecord;
                    while ((eventRecord = reader.ReadEvent()) != null)
                    {
                        DateTime? timeCreated = eventRecord.TimeCreated;
                        if (timeCreated.HasValue)
                        {
                            lock (logonTimes)
                            {
                                logonTimes.Add((DateTime)timeCreated);
                            }
                        }
                    }

                    if (logonTimes.Count > 0)
                    {
                        DateTime lastLogonTime = logonTimes.Max();
                        return lastLogonTime;
                    }
                    else
                    {
                        return DateTime.Now;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return DateTime.Now;
            }
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            await Task.Delay(300);

            if (token.IsCancellationRequested)
                return;

            string searchText = ((TextBox)sender).Text;
            var foundEntries = this.allEntries
                .AsParallel()
                .Where(entry => entry.Name.ToLower().Contains(searchText.ToLower()) || entry.Session_Text.ToLower().StartsWith(searchText.ToLower()))
                .ToArray();

            Dispatcher.Invoke(() =>
            {
                this.BamEntries.Clear();
                foreach (var entry in foundEntries)
                {
                    this.BamEntries.Add(entry);
                }
            });
        }
    }

    public static class CustomCommands
    {
        public static readonly RoutedUICommand PropertiesCommand = new RoutedUICommand(
            "Properties",
            "PropertiesCommand",
            typeof(CustomCommands)
        );

        public static readonly RoutedUICommand HideCommand = new RoutedUICommand(
            "Hide",
            "HideCommand",
            typeof(CustomCommands)
        );
    }
}
