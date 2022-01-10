using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using VisualStudio2022.MarkdownViewer;
using VisualStudio2022.MarkdownViewer.Margin;
using VisualStudio2022.MarkdownViewer.Options;

namespace VisualStudio2022
{
    public partial class MyToolWindowControl : UserControl
    {
        private MarkdownBrowser _markdownBrowser;

        private string _filename;
        private MDocument _mdocument;
        private Browser _browser;
        private Microsoft.CodeAnalysis.Solution _solution;

        public MyToolWindowControl()
        {
            InitializeComponent();
            this.Loaded += MyToolWindowControl_Loaded;
        }

        public MyToolWindowControl(MarkdownBrowserViewModel mbViewModel)
        {
            _filename = mbViewModel.DocumentFileName;
            _mdocument = mbViewModel.MDocument;
            _markdownBrowser = new MarkdownBrowser(mbViewModel);
            //_solution = solution;

            InitializeComponent();
            this.Loaded += MyToolWindowControl_Loaded;
        }

        private void MyToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            panelka.Children.Add(_markdownBrowser.Browser._browser);
            //ppp.Children.Add(Browser._browser);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {

            VS.MessageBox.Show("VisualStudio2022", "Button clicked");
            
        }

        
    }
}