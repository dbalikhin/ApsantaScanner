using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ApsantaScanner.VS2022.ToolWindows
{
    public class IssueViewerWindow : BaseToolWindow<IssueViewerWindow>
    {
        public override string GetTitle(int toolWindowId) => "IssueViewerWindow";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new IssueViewerWindowControl());
        }

        [Guid("44fe3076-2982-4e77-853d-ea87fa03e6ad")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
