using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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

namespace BamChecker
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<BamEntry> BamEntries { get; set; }
        Pages pages;
        bool firstCheck = true;

        public static DateTime sessionDate = DateTime.Now;

        public MainWindow()
        {
            // init
            InitializeComponent();

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
            }

            // automatic filter
            var collectionView = CollectionViewSource.GetDefaultView(BamEntries);
            collectionView.SortDescriptions.Clear();
            collectionView.SortDescriptions.Add(new SortDescription("UTC_Time", ListSortDirection.Descending));
            this.timeCol.SortDirection = ListSortDirection.Descending;

            // session date
            if (firstCheck)
            {
                sessionDate = await GetSystemBootTime();
                firstCheck = false;
            }

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
            entryInteractModal.ShowDialog();
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
                MessageBox.Show(ex.ToString());
                return DateTime.Now;
            }
        }
    }
}
