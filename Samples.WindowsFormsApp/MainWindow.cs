using System;
using System.Windows.Forms;

namespace Samples.WindowsFormsApp
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, EventArgs e)
        {
            var button = ((Button)sender);

            //swap
            var tmp = button.BackColor;
            button.BackColor = button.ForeColor;
            button.ForeColor = tmp;
        }
    }
}
