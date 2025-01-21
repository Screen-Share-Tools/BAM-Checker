using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BamChecker.UI;
using System.Windows.Threading;
using BamChecker.BAM;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.Linq;

namespace BamChecker.Views
{
    public partial class InspectImportsModal : Window
    {
        // const
        private const int DONT_RESOLVE_DLL_REFERENCES = 0x00000001;
        private const int IMAGE_DIRECTORY_ENTRY_IMPORT = 1;

        // vars
        private CancellationTokenSource cancellationTokenSource;

        BamEntry entry { get; set; }
        string exeName { get; set; }
        public ObservableCollection<Import> Imports { set; get; }
        public List<Import> AllImports { set; get; }

        public InspectImportsModal(BamEntry entry, ImageSource icon)
        {
            InitializeComponent();

            this.AllImports = new List<Import>();
            this.Imports = new ObservableCollection<Import>();
            this.DataContext = this;

            this.entry = entry;
            this.exeName = Path.GetFileName(this.entry.Name);
            this.WindowIcon.Source = icon;

            InitTable();

            this.Title = $"{this.exeName} - Imports";
        }

        public async void InitTable()
        {
            await Task.Run(() =>
            {
                try
                {
                    var foundImports = GetImports(this.entry.Name);

                    Dispatcher.Invoke(() =>
                    {
                        foreach (var import in foundImports)
                        {
                            this.Imports.Add(import);
                            this.AllImports.Add(import);
                        }

                        this.Title = $"{this.exeName} - Imports ({Imports.Count})";
                    });
                }
                catch
                {

                }
            });
        }

        // actions
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

            SearchImports();
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

        // func
        public List<Import> GetImports(string filePath)
        {
            List <Import> imports = new List<Import>();

            if (!File.Exists(filePath))
            {
                Pages.Error("File not found.", false);
                this.Close();
            }

                if (!IsDotNetAssembly(filePath))
                {
                    var nativeImports = GetNativeImports(filePath);
                    foreach (var imp in nativeImports)
                    {
                        foreach (var func in imp.Functions)
                        {
                            imports.Add(new Import(imp.LibraryName, func));
                        }
                    }

                    return imports;
                }
                else
                {
                    Pages.Error(".NET executable, cannot check imports.", false);
                    this.Close();
                }

                return imports;
        }

        public bool IsDotNetAssembly(string filePath)
        {
            try
            {
                AssemblyName.GetAssemblyName(filePath);
                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
            catch (FileLoadException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        public List<ImportData> GetNativeImports(string filePath)
        {
            IntPtr hModule = DllImports.LoadLibraryEx(filePath, IntPtr.Zero, DONT_RESOLVE_DLL_REFERENCES);
            if (hModule == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Cannot load PE file. Error: {Marshal.GetLastWin32Error()}");
            }

            try
            {
                IntPtr ntHeaders = DllImports.ImageNtHeader(hModule);
                if (ntHeaders == IntPtr.Zero)
                    throw new InvalidOperationException("NT Headers not found.");

                IntPtr importDescriptor = DllImports.ImageDirectoryEntryToData(hModule, true, IMAGE_DIRECTORY_ENTRY_IMPORT, out uint size);
                if (importDescriptor == IntPtr.Zero || size == 0)
                    throw new InvalidOperationException("Import table not found.");

                var imports = new List<ImportData>();
                while (true)
                {
                    var descriptor = Marshal.PtrToStructure<DllImports.IMAGE_IMPORT_DESCRIPTOR>(importDescriptor);
                    if (descriptor.Name == 0)
                        break;

                    IntPtr namePtr = new IntPtr(hModule.ToInt64() + descriptor.Name);
                    string libraryName = Marshal.PtrToStringAnsi(namePtr);

                    var functions = new List<string>();
                    IntPtr thunk = new IntPtr(hModule.ToInt64() + descriptor.FirstThunk);
                    while (true)
                    {
                        uint functionRva = (uint)Marshal.ReadInt32(thunk);
                        if (functionRva == 0)
                            break;

                        IntPtr functionNamePtr = new IntPtr(hModule.ToInt64() + functionRva + 2);
                        string functionName = Marshal.PtrToStringAnsi(functionNamePtr);
                        functions.Add(functionName);

                        thunk = IntPtr.Add(thunk, IntPtr.Size);
                    }

                    imports.Add(new ImportData
                    {
                        LibraryName = libraryName,
                        Functions = functions
                    });

                    importDescriptor += Marshal.SizeOf<DllImports.IMAGE_IMPORT_DESCRIPTOR>();
                }

                return imports;
            }
            finally
            {
                DllImports.FreeLibrary(hModule);
            }
        }

        public void SearchImports()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    string searchTextLower = SearchTextBox.Text.ToLower();
                    var filteringImports = this.AllImports
                        .Where(import =>
                            (import.LibraryName.ToLower().Contains(searchTextLower) || import.FunctionName.ToLower().Contains(searchTextLower))
                            )
                        .ToList();

                    this.Imports.Clear();
                    foreach (var import in filteringImports)
                    {
                        this.Imports.Add(import);
                    }
                });
            }
            catch (Exception ex)
            {
                Pages.Error(ex.ToString());
            }
        }
    }

    // classes
    public class ImportData
    {
        public string LibraryName { get; set; }
        public List<string> Functions { get; set; }
    }

    public class Import
    {
        public string LibraryName { get; set; }
        public string FunctionName { get; set; }

        public Import(string lib, string func)
        {
            this.LibraryName = lib;
            this.FunctionName = func;
        }
    }
}
