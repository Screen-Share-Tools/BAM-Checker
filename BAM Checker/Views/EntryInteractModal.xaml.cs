using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BAM_Checker.BAM;

namespace BAM_Checker.Views
{
    public partial class EntryInteractModal : Window
    {
        BamEntry entry { set; get; }
        bool isInSession = false;

        public EntryInteractModal(BamEntry entry)
        {
            InitializeComponent();

            this.entry = entry;
            this.txtInput.Text = this.entry.Name;

            isInSession = entry.Local_Time_Date >= MainWindow.sessionDate && entry.Local_Time_Date <= DateTime.Now;
            if (isInSession)
            {
                sessionText.Text = "In Session";
                sessionText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28a745"));
            }
            else
            {
                sessionText.Text = "Not In Session";
                sessionText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc3545"));
            }
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string inputText = txtInput.Text;

            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
