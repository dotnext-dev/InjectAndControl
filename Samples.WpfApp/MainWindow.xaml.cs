using System.Windows;
using System.Windows.Controls;

namespace Samples.WpfApp
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
            var button = ((Button)e.OriginalSource);

            if (button.FontSize < 20)
                button.FontSize = 24;
            else
                button.FontSize = 14;
        }
    }
}
