using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BamChecker.BAM;

namespace BamChecker.Views
{
    public partial class EntryInteractModal : Window
    {
        BamEntry entry { set; get; }

        public EntryInteractModal(BamEntry entry)
        {
            InitializeComponent();

            this.entry = entry;
            this.txtInput.Text = this.entry.Name;

            if (this.entry.Is_In_Session)
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
