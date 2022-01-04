using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ApsantaScanner._2022
{
    public class MyToolWindow : BaseToolWindow<MyToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "My Tool Window";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new MyToolWindowControl());
        }

        [Guid("4cfcc552-a992-478c-aafc-19823883d4d0")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}