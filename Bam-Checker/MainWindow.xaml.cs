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
using System.Reflection;

namespace BamChecker
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;

        public ObservableCollection<BamEntry> BamEntries { get; set; }
        public List<BamEntry> AllEntries { get; set; }

        Pages pages;

        public static DateTime sessionDate = DateTime.Now;

        // filters
        bool hidden_flags, only_session = false;

        public MainWindow()
        {
            // init
            InitializeComponent();

            // version
            var assembly = Assembly.GetExecutingAssembly();
            var informationalVersionAttribute = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if (informationalVersionAttribute != null)
            {
                versionText.Text = $"v{informationalVersionAttribute.InformationalVersion}";
                versionText2.Text = $"v{informationalVersionAttribute.InformationalVersion}";
            }

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
            this.AllEntries = new List<BamEntry>();
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
            sessionDate = await GetSystemBootTime();

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
                if (!entry.Is_Hidden) this.BamEntries.Add(entry);
                this.AllEntries.Add(entry);
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
            this.BamEntries.Clear();
            this.AllEntries.Clear();

            this.pages.Hide("thirdPage");
            this.pages.Show("secondPage");

            // session date
            sessionDate = await GetSystemBootTime();

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
                this.AllEntries.Add(entry);
            }

            SearchBAMEntries();

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
            e.Handled = true;

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

            SearchBAMEntries();
        }

        private void CheckBox_Checked_Hidden_Flags(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            this.hidden_flags = checkbox.IsChecked == true;

            SearchBAMEntries();
        }
        private void CheckBox_Checked_Session_Flags(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;

            if (checkbox.IsChecked == true)
            {
                this.only_session = true;
            }
            else
            {
                this.only_session = false;
            }

            SearchBAMEntries();
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
                    this.AllEntries.Remove(entry);
                    entry.Is_Hidden = true;

                    if (this.hidden_flags) this.BamEntries.Add(entry);
                    this.AllEntries.Add(entry);
                }
            }
        }

        // static
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
                Pages.Error(ex.ToString());
                return DateTime.Now;
            }
        }

        public void SearchBAMEntries()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    string searchTextLower = SearchTextBox.Text.ToLower();
                    var filteringEntries = this.AllEntries
                        .Where(entry =>
                            (entry.Name.ToLower().Contains(searchTextLower) || entry.Session_Text.ToLower().StartsWith(searchTextLower)) &&
                                (this.hidden_flags || !entry.Is_Hidden) &&
                                (!this.only_session || entry.Is_In_Session)
                            )
                        .ToList();

                    this.BamEntries.Clear();
                    foreach (var entry in filteringEntries)
                    {
                        this.BamEntries.Add(entry);
                    }
                });
            }
            catch (Exception ex)
            {
                Pages.Error(ex.ToString());
            }
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
