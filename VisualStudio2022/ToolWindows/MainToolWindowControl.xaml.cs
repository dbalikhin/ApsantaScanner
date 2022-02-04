using ApstantaScanner.Vsix.Shared.Auth;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
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
                
            }
            if (e.NewStatus == AuthStatus.DeviceCodeReceived)
            {
                // provide instructions to enter the code in a browser
                textBlockDeviceFlowStatus.Text = "Enter the code: " + e.UserCode;
            }
            else if (e.NewStatus == AuthStatus.TokenReceived)
            {
                textBlockDeviceFlowStatus.Text = "Token received";
                // Update UI
                SetControls(true);
            }
            else if (e.NewStatus == AuthStatus.Error)
            {                
                textBlockError.Text = e.ErrorMessage;
                SetControls(false);
                textBlockDeviceFlowStatus.Text = "Error occured";
            }
        }

        public void UpdateBrowser(string text)
        {
            var mdoc = new MDocument(text);
            _markdownBrowser.Browser.UpdateBrowserAsync(mdoc).FireAndForget();
        }

        private void SetControls(bool isAuthorized)
        {
            if (isAuthorized)
            {
                checkBoxAuthStatus.IsChecked = true;
                checkBoxAuthStatus.Content = "Authorized";
                buttonAuthenticate.Content = "Re-authorize";
                textBlockDeviceFlowStatus.Text = "Completed";
                checkBoxAuthStatus.IsEnabled = false;
            }
            else
            {
                checkBoxAuthStatus.IsChecked = false;             
                checkBoxAuthStatus.Content = "Not Authorized";
                buttonAuthenticate.Content = "Authorize";
                textBlockDeviceFlowStatus.Text = "Not Requested";
                checkBoxAuthStatus.IsEnabled = false;
            }
            buttonAuthenticate.IsEnabled = true;

        }

        private void MainToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            // check for auth status
            var isAuthorized = !string.IsNullOrEmpty(_authService.UserToken);
            SetControls(isAuthorized);
            
            
            if (!BrowserRow.Children.Contains(_markdownBrowser.Browser._browser))
            {
                BrowserRow.Children.Add(_markdownBrowser.Browser._browser);
            }
        }
        
        // TODO: https://stackoverflow.com/questions/12556993/significance-of-declaring-a-wpf-event-handler-as-async-in-c-sharp-5
        private async void buttonAuthenticate_Click(object sender, RoutedEventArgs e)
        {
            if (_authService.AuthStatus == AuthStatus.NotStarted || _authService.AuthStatus == AuthStatus.TokenReceived || _authService.AuthStatus == AuthStatus.Error)
            {
                textBlockDeviceFlowStatus.Text = "Started";
                buttonAuthenticate.IsEnabled = false;
                await _authService.InitiateDeviceFlowAsync();
            }
        }
    }



}