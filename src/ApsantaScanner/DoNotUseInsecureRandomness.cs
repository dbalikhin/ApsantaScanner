// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.Caching;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Apsanta.Scanner
{
 
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotUseInsecureRandomness : DiagnosticAnalyzer
    {
        // modified id, just in case
        internal const string DiagnosticId = "ASCA5394";

        internal static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorHelper.Create(
            DiagnosticId,
            "DoNotUseInsecureRandomness",
            "DoNotUseInsecureRandomness",
            DiagnosticCategory.Security,
            RuleLevel.BuildWarning,
            description: "DoNotUseInsecureRandomness",
            isPortedFxCopRule: false,
            isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Security analyzer - analyze and report diagnostics on generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
            {
                if (!compilationStartAnalysisContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRandom, out var randomTypeSymbol))
                {
                    return;
                }

                compilationStartAnalysisContext.RegisterOperationAction(operationAnalysisContext =>
                {
                    var invocationOperation = (IInvocationOperation)operationAnalysisContext.Operation;
                    var typeSymbol = invocationOperation.TargetMethod.ContainingType;

                    if (randomTypeSymbol.Equals(typeSymbol))
                    {
                        var diagnostic = invocationOperation.CreateDiagnostic(
                                Rule,
                                typeSymbol.Name);

                        ObjectCache cache = MemoryCache.Default;
                        CacheItemPolicy policy = new CacheItemPolicy();
                        policy.AbsoluteExpiration =
                            DateTimeOffset.Now.AddMinutes(10.0);

                        cache.Set("mydiagnostic", diagnostic, policy);

                        operationAnalysisContext.ReportDiagnostic(diagnostic);
                    }
                }, OperationKind.Invocation);
            });
        }
    }
}