using ApstantaScanner.Vsix.Shared.ErrorList;

namespace VisualStudio2022.MarkdownViewer.Margin
{

    public class MarkdownBrowserViewModel
    {
        public string DocumentFileName { get; set; }

        public MDocument MDocument { get; set; }

        public DiagnosticItem DiagnosticItem { get; set; }
    }
}
