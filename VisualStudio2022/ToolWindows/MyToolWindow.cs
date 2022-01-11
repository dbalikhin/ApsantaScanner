using ApsantaScanner.Vsix.Shared.ErrorList;
using ApstantaScanner.Vsix.Shared.ErrorList;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VisualStudio2022.MarkdownViewer;
using VisualStudio2022.MarkdownViewer.Margin;

namespace VisualStudio2022
{
    public class MyToolWindow : BaseToolWindow<MyToolWindow>
    {
        private readonly IErrorListEventSelectionService errorListEventSelectionService;

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
            private IErrorListEventSelectionService _errorListEventSelectionService;
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;

                var componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
                if (componentModel != null)
                {
                    _errorListEventSelectionService = componentModel.GetService<IErrorListEventSelectionService>();
                    
                    _errorListEventSelectionService.NavigatedItemChanged += _errorListEventSelectionService_NavigatedItemChanged;
                }
                
            }

            private void _errorListEventSelectionService_NavigatedItemChanged(object sender, ErrorListSelectionChangedEventArgs e)
            {
                if (sender is ErrorListEventProcessor)
                {
                    var navigatedItem = (sender as ErrorListEventProcessor).NavigatedItem;
                    
                    if (navigatedItem != null)
                    {
                        var report = GenerateReport(navigatedItem.DiagnosticItem);
                        (Content as MyToolWindowControl).UpdateBrowser(report);
                    }
                    
                }

                //(Content as MyToolWindowControl).Refresh();
                
            }

            private string GenerateReport(DiagnosticItem diag)
            {
                StringBuilder sb = new();
                sb.AppendLine("## SQL Injection")
                  .AppendLine("")
                  .AppendLine("ErrorText: " + diag.ErrorText)
                  .AppendLine("ErrorCode: " + diag.ErrorCode)
                  .AppendLine("Line: " + diag.Line.ToString()
                );
                return sb.ToString();
            }
        }
    }
}