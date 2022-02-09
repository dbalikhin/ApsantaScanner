﻿using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ApsantaScanner.Config;
using ApsantaScanner.Security.Locale;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;


namespace ApsantaScanner
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class CompilationCompletedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCS0000";

        public static readonly DiagnosticDescriptor Rule = LocaleUtil.GetDescriptor(DiagnosticId);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (!Debugger.IsAttached) // prefer single thread for debugging in development
                context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {   
                var timer = Stopwatch.StartNew();
                var configuration = Configuration.GetOrCreate(ctx);
                if (configuration.ReportAnalysisCompletion)
                {
                    
                    ctx.RegisterCompilationEndAction(ctx =>
                    {
                        try
                        {
                            MultiThreadFileWriter.Instance.WriteLine("Analysis time: " + timer.ElapsedMilliseconds + " ms.");
                            timer.Stop();
                            MultiThreadFileWriter.Instance.WriteToFileSync();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                        //Task.Run(MultiThreadFileWriter.Instance.WriteToFile, default(CancellationToken));
                        ctx.ReportDiagnostic(Diagnostic.Create(Rule, Location.None, ctx.Compilation.AssemblyName));
                    });
                }
            });
        }
    }
}
