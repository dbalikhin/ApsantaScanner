using System.Windows;
using System.Windows.Controls;

namespace VisualStudio2022
{
    public partial class MyToolWindowControl : UserControl
    {
        public MyToolWindowControl()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            VS.MessageBox.Show("VisualStudio2022", "Button clicked");
        }
    }
}