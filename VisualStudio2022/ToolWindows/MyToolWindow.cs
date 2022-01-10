using EnvDTE80;
using Microsoft.VisualStudio.Imaging;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VisualStudio2022.MarkdownViewer;
using VisualStudio2022.MarkdownViewer.Margin;

namespace VisualStudio2022
{
    public class MyToolWindow : BaseToolWindow<MyToolWindow>
    {
  

        public override string GetTitle(int toolWindowId) => "My Tool Window";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var sol = (Package as VisualStudio2022Package).CurrentSolution;
            var mbViewModel = new MarkdownBrowserViewModel() { MDocument = new MDocument("# Headline 1 \r\n\r\n Normal Text"), DocumentFileName = "fake" };
            return Task.FromResult<FrameworkElement>(new MyToolWindowControl(mbViewModel));
        }

        

        [Guid("448df334-be26-4c43-95c9-289e20530261")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}