global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using VisualStudio2022.MarkdownViewer.Margin;

namespace VisualStudio2022
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideToolWindow(typeof(MyToolWindow.Pane), Style = VsDockStyle.Float, Window = WindowGuids.MainWindow)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.VisualStudio2022String)]
    public sealed class VisualStudio2022Package : ToolkitPackage
    {
        public Microsoft.CodeAnalysis.Solution CurrentSolution { get; private set; }
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var workspace = (Workspace)componentModel.GetService<VisualStudioWorkspace>();
            CurrentSolution = workspace.CurrentSolution;

            foreach (var project in workspace.CurrentSolution.Projects)
            {
                
                var compilation = await project.GetCompilationAsync();

                var diagnostics = compilation?.GetDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Hidden);
                if (diagnostics != null)
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        //diagnostic.Location.GetMappedLineSpan().
                    }
                }
                  
                
            }
            await this.RegisterCommandsAsync();
            


            this.RegisterToolWindows();
        }
    }
}