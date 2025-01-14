using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BAM_Checker.UI;
using BAM_Checker.BAM;
using System.ComponentModel;
using BAM_Checker.Views;
using System.Collections.Concurrent;

namespace BAM_Checker
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
                sessionDate = GetSystemBootTime();
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

            string name = (string)item.GetType().GetProperty("Name")?.GetValue(item)!;
            BamEntry entry = this.BamEntries.ToList().Find(x => x.Name == name)!;
            if (entry == null) return;

            var entryInteractModal = new EntryInteractModal(entry);
            entryInteractModal.ShowDialog();
        }

        // static
        static public DateTime GetSystemBootTime()
        {
            try
            {
                EventLog eventLog = new EventLog("Security");
                var logonTimes = new ConcurrentBag<DateTime>();

                Parallel.ForEach(eventLog.Entries.Cast<EventLogEntry>(), entry =>
                {
                    if (entry.InstanceId == 4624 && entry.Message.Contains(Environment.UserName))
                    {
                        logonTimes.Add(entry.TimeGenerated);
                    }
                });

                if (logonTimes.Count > 0)
                {
                    DateTime lastLogonTime = logonTimes.Max();
                    return lastLogonTime;
                }
                else
                {
                    return DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                return DateTime.Now;
            }
        }
    }
}