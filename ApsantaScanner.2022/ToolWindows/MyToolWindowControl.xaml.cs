using System.Windows;
using System.Windows.Controls;

namespace ApsantaScanner._2022
{
    public partial class MyToolWindowControl : UserControl
    {
        public MyToolWindowControl()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            VS.MessageBox.Show("ApsantaScanner._2022", "Button clicked");
        }
    }
}