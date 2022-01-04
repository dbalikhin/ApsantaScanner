using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using VisualStudio2022.MarkdownViewer.Options;

namespace VisualStudio2022.MarkdownViewer.Margin
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(nameof(MarkdownPreviewMarginProvider))]
    [Order(After = PredefinedMarginNames.RightControl)]
    [MarginContainer(PredefinedMarginNames.Right)]
    [ContentType("Markdown")]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)] // This is to prevent the margin from loading in the diff view
    public class MarkdownPreviewMarginProvider : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            if (!AdvancedOptions.Instance.EnablePreviewWindow)
            {
                return null;
            }

            return wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(() => new BrowserMargin(wpfTextViewHost.TextView));
        }
    }
}
