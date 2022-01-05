using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using VisualStudio2022.MarkdownViewer.Margin;

namespace VisualStudio2022
{
    public partial class MyToolWindowControl : UserControl
    {
        [Import(typeof(MarkdownBrowser))]
        public MarkdownBrowser MarkdownBrowser { get; set; }

        public MyToolWindowControl()
        {
            InitializeComponent();
            this.Loaded += MyToolWindowControl_Loaded;
        }

        private void MyToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (MarkdownBrowser != null)
            {

            }
            //ppp.Children.Add(Browser._browser);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            VS.MessageBox.Show("VisualStudio2022", "Button clicked");
            
        }

        
    }
}