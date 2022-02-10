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
using System.Linq;
using System.Threading.Tasks;
using ApstantaScanner.Vsix.Shared.ErrorList;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using ApsantaScanner.Security.Taint;
using System.Reflection;
using Microsoft.VisualStudio.Shell.Interop;


namespace VisualStudio2022
{
    //https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.vsdockstyle?view=visualstudiosdk-2022
    //[ProvideToolWindow(typeof(MainToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.SolutionExplorer)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideToolWindow(typeof(MainToolWindow.Pane), Style = VsDockStyle.MDI, Transient = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideToolWindowVisibility(typeof(MainToolWindow.Pane), VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]
    [Guid(PackageGuids.VisualStudio2022String)]
    public sealed class VisualStudio2022Package : ToolkitPackage
    {
        public Microsoft.CodeAnalysis.Solution CurrentSolution { get; private set; }
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // https://github.com/microsoft/sarif-visualstudio-extension/blob/main/src/Sarif.Viewer.VisualStudio.Core/SarifViewerPackage.cs
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var workspace = (Workspace)componentModel.GetService<VisualStudioWorkspace>();
            VS.Events.BuildEvents.SolutionBuildDone += BuildEvents_SolutionBuildDone;



            CurrentSolution = workspace.CurrentSolution;

            AuthServiceInstance = new AuthService();

            
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

        private void BuildEvents_SolutionBuildDone(bool obj)
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var workspace = (Workspace)componentModel.GetService<VisualStudioWorkspace>();
            var ss = workspace.CurrentSolution.AnalyzerReferences;
            var s = workspace.Services.PersistentStorage.GetStorage(workspace.CurrentSolution);

            foreach (var project in workspace.CurrentSolution.Projects)
            {

                var compilation = await project.GetCompilationAsync().Result;

                var diagnostics = compilation?.GetDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Hidden);
                if (diagnostics != null)
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        //diagnostic.Location.GetMappedLineSpan().
                    }
                }


            }

        }

        // https://github.com/microsoft/VSSDK-Extensibility-Samples/blob/master/SolutionLoadEvents/README.md
        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));

            return value is bool isSolOpen && isSolOpen;
        }

        private void HandleOpenSolution(object sender = null, EventArgs e = null)
        {
        }

        public AuthService AuthServiceInstance { get; private set; }

        public async Task<Diagnostic> GetDiagnosticDetailsAsync(DiagnosticItem diagnosticItem)
        {
            var project = CurrentSolution.Projects.FirstOrDefault(p => p.Name == diagnosticItem.ProjectName);
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);   
            var diag = compilation?.GetDiagnostics().Where(d=> d.Severity != DiagnosticSeverity.Hidden).ToList();
            var diagnostic = compilation?.GetDiagnostics().FirstOrDefault(d => d.Id == diagnosticItem.ErrorCode);
            /*
            List<DiagnosticAnalyzer> analyzers = new();
            var types = typeof(PathTraversalTaintAnalyzer).GetTypeInfo().Assembly.DefinedTypes;
            foreach (var type in types)
            {
                if (type.IsAbstract)
                    continue;

                var secAttributes = type.GetCustomAttributes(typeof(DiagnosticAnalyzerAttribute), false)
                                            .Cast<DiagnosticAnalyzerAttribute>();
                foreach (var attribute in secAttributes)
                {
                    var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(type.AsType());

                    analyzers.Add(analyzer);
                    break;
                }
            }


            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers.ToImmutableArray(), project.AnalyzerOptions);
            var ds =  await compilationWithAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);
            var diagnostic = ds.FirstOrDefault(d => d.Id == diagnosticItem.ErrorCode); 

            //D
            */
            return diagnostic;
        }
    }
}