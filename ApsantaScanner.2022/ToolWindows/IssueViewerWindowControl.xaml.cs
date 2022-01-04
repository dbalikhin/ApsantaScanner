using System.Windows;
using System.Windows.Controls;

namespace ApsantaScanner.VS2022.ToolWindows
{
    public partial class IssueViewerWindowControl : UserControl
    {
        public IssueViewerWindowControl()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            VS.MessageBox.Show("IssueViewerWindowControl", "Button clicked");
        }

        private void BoomButton_Click(object sender, RoutedEventArgs e)
        {
            VS.MessageBox.Show("IssueViewerWindowControl", "Boom Button clicked");
        }
    }
}
