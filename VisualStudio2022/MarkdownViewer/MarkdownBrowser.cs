using Microsoft.VisualStudio.PlatformUI;

namespace VisualStudio2022.MarkdownViewer
{

    public class MarkdownBrowser
    {
        private readonly MDocument _mdocument;
      
        private bool _isDisposed;

        public Browser Browser { get; private set; }

        public MarkdownBrowser(MarkdownBrowserViewModel mbViewModel)
        {
            _mdocument = mbViewModel.MDocument;
            Browser = new Browser(mbViewModel.DocumentFileName, mbViewModel.MDocument);

           
            UpdateBrowser(_mdocument);

            _mdocument.Parsed += UpdateBrowser;
         

            //SetResourceReference(BackgroundProperty, VsBrushes.ToolWindowBackgroundKey);
            //Browser._browser.SetResourceReference(BackgroundProperty, VsBrushes.ToolWindowBackgroundKey);
            VSColorTheme.ThemeChanged += OnThemeChange;
        }

        private void OnThemeChange(ThemeChangedEventArgs e)
        {
            RefreshAsync().FireAndForget();
        }



        public async Task RefreshAsync()
        {
            await Browser.RefreshAsync();
        }


        private void UpdateBrowser(MDocument mdocument)
        {
            if (!mdocument.IsParsing)
            {
                Browser.UpdateBrowserAsync(mdocument).FireAndForget();
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _mdocument.Parsed -= UpdateBrowser; 
                VSColorTheme.ThemeChanged -= OnThemeChange;
                Browser?.Dispose();
            }

            _isDisposed = true;
        }
    }
}
