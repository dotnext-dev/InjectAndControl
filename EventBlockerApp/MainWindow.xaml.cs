using Injector.Common;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace EventBlockerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName(((ComboBoxItem)appsCmbBox.SelectedItem).Content.ToString());

            foreach (Process p in processes)
            {
                IntPtr windowHandle = p.MainWindowHandle;

                (new WindowInfo(windowHandle)).Inject();
            }
        }
    }
}
