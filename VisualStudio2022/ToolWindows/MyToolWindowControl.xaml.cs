using EnvDTE;
using GitHub.Authentication.CredentialManagement;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using VisualStudio2022.Auth;
using VisualStudio2022.MarkdownViewer;
using VisualStudio2022.MarkdownViewer.Margin;
using VisualStudio2022.MarkdownViewer.Options;

namespace VisualStudio2022
{
    public partial class MyToolWindowControl : UserControl
    {
        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        public MarkdownBrowserViewModel MarkdownBrowserViewModel { get; set; }
        MarkdownBrowser _markdownBrowser;

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

        public void UpdateBrowser(string text)
        {
            var mdoc = new MDocument(text);
            _markdownBrowser.Browser.UpdateBrowserAsync(mdoc).FireAndForget();
        }

        private void MyToolWindowControl_Loaded(object sender, RoutedEventArgs e)
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
            //AuthService.Main.
            
            //Credential.

            VS.MessageBox.Show("VisualStudio2022", "Button clicked");
            
        }

  
        private async void btnAuth_Click(object sender, RoutedEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            using var client = new HttpClient();
            
            var authorizationResponse = await AuthService.StartDeviceFlowAsync(client);
            lAuthCode.Content = authorizationResponse.DeviceCode;
            //AuthService.OpenWebPage(authorizationResponse.VerificationUri);
            var browser = _serviceProvider.GetService(typeof(SVsWebBrowsingService)) as IVsWebBrowsingService;
            if (browser == null)
            {
                Debug.Fail("Failed to get SVsWebBrowsingService service.");
                return VSConstants.E_UNEXPECTED;
            }

            IVsWindowFrame frame = null;

            int hr = browser.Navigate(authorizationResponse.VerificationUri, );
            //itemOps.Navigate(authorizationResponse.VerificationUri);
            var tokenResponse = await AuthService.GetTokenAsync(client, authorizationResponse);

            
            
        }

     
    }



}