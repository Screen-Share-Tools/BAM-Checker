﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using System.Xml.Serialization;
using BAM_Checker;

namespace BAM_Checker.UI
{
    internal class Pages
    {    

        MainWindow mainWindow;
        List<Page> pages;

        public Pages(MainWindow mainWindow) {
            this.mainWindow = mainWindow;
            this.pages = new List<Page>();
        }

        // methods
        public void Add(string name, UIElement element)
        {
            if (name == null || element == null ) {
                Error("Element not found");
                return;
            }

            if (pages.Count == 0) element.Visibility = Visibility.Visible;
            else element.Visibility = Visibility.Collapsed;

            this.pages.Add(new Page { element = element, name = name });
        }

        public void Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Error("Element not found");
                return;
            }

            Page page = this.pages.Find(page => page.name == name);
            if (page.name == null)
            {
                Error("Page not found.");
                return;
            }

            this.pages.Remove(page);
        }

        public void Show(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Error("Element not found");
                return;
            }

            Page page = this.pages.Find(page => page.name == name);
            if (page.name == null)
            {
                Error("Page not found.");
                return;
            }

            page.element.Visibility = Visibility.Visible;
        }

        public void Hide(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Error("Element not found");
                return;
            }

            Page page = this.pages.Find(page => page.name == name);
            if (page.name == null)
            {
                Error("Page not found.");
                return;
            }

            page.element.Visibility = Visibility.Collapsed;
        }

        // err
        public static void Error(string msg)
        {
            MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }

    public struct Page
    {
        public string name;
        public UIElement element;
    }
}
