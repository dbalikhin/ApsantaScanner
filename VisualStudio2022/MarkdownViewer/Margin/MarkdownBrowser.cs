using System.ComponentModel.Composition;

namespace VisualStudio2022.MarkdownViewer.Margin
{
    [Export(typeof(MarkdownBrowser))]
    public class MarkdownBrowser
    {
        public Browser Browser { get; private set; }

        public MarkdownBrowser()
        {
            Browser = new Browser(null, null);
        }
    }
}
