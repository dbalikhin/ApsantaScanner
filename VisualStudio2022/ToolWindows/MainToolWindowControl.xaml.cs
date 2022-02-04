using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using VisualStudio2022.Auth;
using VisualStudio2022.MarkdownViewer;

namespace VisualStudio2022
{
    public partial class MainToolWindowControl : UserControl
    {
        [Import]
        internal SVsServiceProvider ServiceProvider = null;
        private AuthService _authService;

        public MarkdownBrowserViewModel MarkdownBrowserViewModel { get; set; }
        MarkdownBrowser _markdownBrowser;

        private string _filename;
        private MDocument _mdocument;
        private Browser _browser;
        private Microsoft.CodeAnalysis.Solution _solution;

        public MainToolWindowControl()
        {
            InitializeComponent();
            this.Loaded += MainToolWindowControl_Loaded;
        }

        public MainToolWindowControl(MarkdownBrowserViewModel mbViewModel, AuthService authService)
        {
            _authService = authService;
            _filename = mbViewModel.DocumentFileName;
            _mdocument = mbViewModel.MDocument;
            _markdownBrowser = new MarkdownBrowser(mbViewModel);
            //_solution = solution;

            InitializeComponent();
            this.Loaded += MainToolWindowControl_Loaded;
            _authService.GithubAuthStatusChanged += AuthService_GithubAuthStatusChanged;
        }

        private void AuthService_GithubAuthStatusChanged(object sender, GithubAuthStatusChangedEventArgs e)
        {
            if (e.NewStatus == AuthStatus.NotStarted)
            {
                lStatus.Content = "Not Started";
            }
            if (e.NewStatus == AuthStatus.DeviceCodeReceived)
            {
                // provide instructions to enter the code in a browser
                lStatus.Content = "DeviceCodeReceived : " + e.UserCode;
            }
            else if (e.NewStatus == AuthStatus.TokenReceived)
            {
                // Update UI
                lStatus.Content = "TokenReceived : " + e.UserToken;
            }
            else if (e.NewStatus != AuthStatus.Error)
            {
                lStatus.Content = "Error boo!!!!";
            }
        }

        public void UpdateBrowser(string text)
        {
            var mdoc = new MDocument(text);
            _markdownBrowser.Browser.UpdateBrowserAsync(mdoc).FireAndForget();
        }

        private void MainToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            //mBrowser = _markdownBrowser.Browser._browser;
            
            if (!BrowserRow.Children.Contains(_markdownBrowser.Browser._browser))
            {
                BrowserRow.Children.Add(_markdownBrowser.Browser._browser);
            }
            //ppp.Children.Add(Browser._browser);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {

            VS.MessageBox.Show("VisualStudio2022", "Button clicked");
            
        }

        // TODO: https://stackoverflow.com/questions/12556993/significance-of-declaring-a-wpf-event-handler-as-async-in-c-sharp-5
        private async void btnAuth_Click(object sender, RoutedEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (string.IsNullOrEmpty(_authService.UserToken) && _authService.AuthStatus == AuthStatus.NotStarted)
            {
                await _authService.InitiateDeviceFlowAsync();
            } 
        }

     
    }



}