global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using ApstantaScanner.Vsix.Shared.Auth;

namespace VisualStudio2022
{
    //https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.vsdockstyle?view=visualstudiosdk-2022
    //[ProvideToolWindow(typeof(MainToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.SolutionExplorer)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideToolWindow(typeof(MainToolWindow.Pane), Style = VsDockStyle.MDI, Transient = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideToolWindowVisibility(typeof(MainToolWindow.Pane), VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]
    [Guid(PackageGuids.VisualStudio2022String)]
    public sealed class VisualStudio2022Package : ToolkitPackage
    {
        public Microsoft.CodeAnalysis.Solution CurrentSolution { get; private set; }
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var workspace = (Workspace)componentModel.GetService<VisualStudioWorkspace>();
            CurrentSolution = workspace.CurrentSolution;

            AuthServiceInstance = new AuthService();

            /*
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
            */
            await this.RegisterCommandsAsync();
            


            this.RegisterToolWindows();
        }

        public AuthService AuthServiceInstance { get; private set; }
    }
}