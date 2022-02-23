// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis;
using Analyzer.Utilities.PooledObjects;
using ApsantaScanner.Config;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ApsantaScanner.Security
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Base class to aid in implementing tainted data analyzers.
    /// </summary>
    public abstract class SourceTriggeredTaintedDataAnalyzerBase : DiagnosticAnalyzer
    {
        /// <summary>
        /// <see cref="DiagnosticDescriptor"/> for when tainted data enters a sink.
        /// </summary>
        /// <remarks>Format string arguments are:
        /// 0. Sink symbol.
        /// 1. Method name containing the code where the tainted data enters the sink.
        /// 2. Source symbol.
        /// 3. Method name containing the code where the tainted data came from the source.
        /// </remarks>
        protected abstract DiagnosticDescriptor TaintedDataEnteringSinkDescriptor { get; }

        /// <summary>
        /// Kind of tainted data sink.
        /// </summary>
        protected abstract SinkKind SinkKind { get; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(TaintedDataEnteringSinkDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            if (!Debugger.IsAttached)
                context.EnableConcurrentExecution();

            // do not analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            //context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(
                (compilationContext) =>
                {
                    Compilation compilation = compilationContext.Compilation;
                    var config = Configuration.GetOrCreate(compilationContext);
                    //TaintedDataConfig taintedDataConfig = TaintedDataConfig.GetOrCreate(compilation);
                    TaintedDataSymbolMap<SourceInfo> sourceInfoSymbolMap = config.TaintConfiguration.GetSourceSymbolMap(SinkKind);
                    if (sourceInfoSymbolMap.IsEmpty)
                    {
                        return;
                    }

                    TaintedDataSymbolMap<SinkInfo> sinkInfoSymbolMap = config.TaintConfiguration.GetSinkSymbolMap(SinkKind);
                    if (sinkInfoSymbolMap.IsEmpty)
                    {
                        return;
                    }

                    compilationContext.RegisterOperationBlockStartAction(
                        operationBlockStartContext =>
                        {
                            ISymbol owningSymbol = operationBlockStartContext.OwningSymbol;
                            AnalyzerOptions options = operationBlockStartContext.Options;
                            CancellationToken cancellationToken = operationBlockStartContext.CancellationToken;
                            if (options.IsConfiguredToSkipAnalysis(TaintedDataEnteringSinkDescriptor, owningSymbol, compilation))
                            {
                                return;
                            }

                            WellKnownTypeProvider wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
                            Lazy<ControlFlowGraph?> controlFlowGraphFactory = new Lazy<ControlFlowGraph?>(
                                () => operationBlockStartContext.OperationBlocks.GetControlFlowGraph());
                            Lazy<PointsToAnalysisResult?> pointsToFactory = new Lazy<PointsToAnalysisResult?>(
                                () =>
                                {
                                    if (controlFlowGraphFactory.Value == null)
                                    {
                                        return null;
                                    }

                                    InterproceduralAnalysisConfiguration interproceduralAnalysisConfiguration = InterproceduralAnalysisConfiguration.Create(
                                                                    options,
                                                                    SupportedDiagnostics,
                                                                    controlFlowGraphFactory.Value,
                                                                    operationBlockStartContext.Compilation,
                                                                    defaultInterproceduralAnalysisKind: InterproceduralAnalysisKind.ContextSensitive);
                                    return PointsToAnalysis.TryGetOrComputeResult(
                                                                controlFlowGraphFactory.Value,
                                                                owningSymbol,
                                                                options,
                                                                wellKnownTypeProvider,
                                                                PointsToAnalysisKind.Complete,
                                                                interproceduralAnalysisConfiguration,
                                                                interproceduralAnalysisPredicate: null);
                                });
                            Lazy<(PointsToAnalysisResult?, ValueContentAnalysisResult?)> valueContentFactory = new Lazy<(PointsToAnalysisResult?, ValueContentAnalysisResult?)>(
                                () =>
                                {
                                    if (controlFlowGraphFactory.Value == null)
                                    {
                                        return (null, null);
                                    }

                                    InterproceduralAnalysisConfiguration interproceduralAnalysisConfiguration = InterproceduralAnalysisConfiguration.Create(
                                                                    options,
                                                                    SupportedDiagnostics,
                                                                    controlFlowGraphFactory.Value,
                                                                    operationBlockStartContext.Compilation,
                                                                    defaultInterproceduralAnalysisKind: InterproceduralAnalysisKind.ContextSensitive);
                                    ValueContentAnalysisResult? valuecontentAnalysisResult = ValueContentAnalysis.TryGetOrComputeResult(
                                                                    controlFlowGraphFactory.Value,
                                                                    owningSymbol,
                                                                    options,
                                                                    wellKnownTypeProvider,
                                                                    PointsToAnalysisKind.Complete,
                                                                    interproceduralAnalysisConfiguration,
                                                                    out _,
                                                                    out PointsToAnalysisResult? p);

                                    return (p, valuecontentAnalysisResult);
                                });

                            PooledHashSet<IOperation> rootOperationsNeedingAnalysis = PooledHashSet<IOperation>.GetInstance();

                            operationBlockStartContext.RegisterOperationAction(
                                operationAnalysisContext =>
                                {
                                    IPropertyReferenceOperation propertyReferenceOperation = (IPropertyReferenceOperation)operationAnalysisContext.Operation;
                                    if (sourceInfoSymbolMap.IsSourceProperty(propertyReferenceOperation.Property))
                                    {
                                        lock (rootOperationsNeedingAnalysis)
                                        {
                                            rootOperationsNeedingAnalysis.Add(propertyReferenceOperation.GetRoot());
                                        }
                                    }
                                },
                                OperationKind.PropertyReference);

                            if (sourceInfoSymbolMap.RequiresParameterReferenceAnalysis)
                            {
                                operationBlockStartContext.RegisterOperationAction(
                                    operationAnalysisContext =>
                                    {
                                        IParameterReferenceOperation parameterReferenceOperation = (IParameterReferenceOperation)operationAnalysisContext.Operation;
                                        if (sourceInfoSymbolMap.IsSourceParameter(parameterReferenceOperation.Parameter, wellKnownTypeProvider))
                                        {
                                            lock (rootOperationsNeedingAnalysis)
                                            {
                                                rootOperationsNeedingAnalysis.Add(parameterReferenceOperation.GetRoot());
                                            }
                                        }
                                    },
                                    OperationKind.ParameterReference);
                            }

                            operationBlockStartContext.RegisterOperationAction(
                                operationAnalysisContext =>
                                {
                                    IInvocationOperation invocationOperation = (IInvocationOperation)operationAnalysisContext.Operation;
                                    if (sourceInfoSymbolMap.IsSourceMethod(
                                            invocationOperation.TargetMethod,
                                            invocationOperation.Arguments,
                                            pointsToFactory,
                                            valueContentFactory,
                                            out _))
                                    {
                                        lock (rootOperationsNeedingAnalysis)
                                        {
                                            rootOperationsNeedingAnalysis.Add(invocationOperation.GetRoot());
                                        }
                                    }
                                },
                                OperationKind.Invocation);

                            if (config.TaintConfiguration.HasTaintArraySource(SinkKind, config))
                            {
                                operationBlockStartContext.RegisterOperationAction(
                                    operationAnalysisContext =>
                                    {
                                        IArrayInitializerOperation arrayInitializerOperation = (IArrayInitializerOperation)operationAnalysisContext.Operation;
                                        if (arrayInitializerOperation.GetAncestor<IArrayCreationOperation>(OperationKind.ArrayCreation)?.Type is IArrayTypeSymbol arrayTypeSymbol
                                            && sourceInfoSymbolMap.IsSourceConstantArrayOfType(arrayTypeSymbol, arrayInitializerOperation))
                                        {
                                            lock (rootOperationsNeedingAnalysis)
                                            {
                                                rootOperationsNeedingAnalysis.Add(operationAnalysisContext.Operation.GetRoot());
                                            }
                                        }
                                    },
                                    OperationKind.ArrayInitializer);
                            }

                            operationBlockStartContext.RegisterOperationBlockEndAction(
                                operationBlockAnalysisContext =>
                                {
                                    try
                                    {
                                        lock (rootOperationsNeedingAnalysis)
                                        {
                                            if (!rootOperationsNeedingAnalysis.Any())
                                            {
                                                return;
                                            }

                                            if (controlFlowGraphFactory.Value == null)
                                            {
                                                return;
                                            }

                                            foreach (IOperation rootOperation in rootOperationsNeedingAnalysis)
                                            {
                                                TaintedDataAnalysisResult? taintedDataAnalysisResult = TaintedDataAnalysis.TryGetOrComputeResult(
                                                    controlFlowGraphFactory.Value,
                                                    operationBlockAnalysisContext.Compilation,
                                                    operationBlockAnalysisContext.OwningSymbol,
                                                    operationBlockAnalysisContext.Options,
                                                    TaintedDataEnteringSinkDescriptor,
                                                    sourceInfoSymbolMap,
                                                    config.TaintConfiguration.GetSanitizerSymbolMap(SinkKind),
                                                    sinkInfoSymbolMap);
                                                if (taintedDataAnalysisResult == null)
                                                {
                                                    return;
                                                }

                                                foreach (TaintedDataSourceSink sourceSink in taintedDataAnalysisResult.TaintedDataSourceSinks)
                                                {
                                                    if (!sourceSink.SinkKinds.Contains(SinkKind))
                                                    {
                                                        continue;
                                                    }

                                                    // https://github.com/dotnet/roslyn-analyzers/blob/main/docs/Writing%20dataflow%20analysis%20based%20analyzers.md

                                                    foreach (SymbolAccess sourceOrigin in sourceSink.SourceOrigins)
                                                    {
                                                        var initialTaintedOperations = taintedDataAnalysisResult.GetTaintedOperations();
                                                        List<IOperation> taintedOperations = new();
                                                        AddCurrentLevelResults(initialTaintedOperations, taintedDataAnalysisResult, sourceSink, sourceOrigin.Location);


                                                        // behold, poor man taint analysis data flow reconstruction from source to sink and back to source to catch all expression statements and assignments
                                                        void AddCurrentLevelResults(List<IOperation> operations, DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> taintedResult, TaintedDataSourceSink sourceSink, Location sourceLocation)
                                                        {
                                                            // identify new source if required
                                                            //operations.Where(o => o.Kind == OperationKind.ParameterReference).
                                                            var invocationOperation = operations.FirstOrDefault(op => op.Kind is OperationKind.Invocation or OperationKind.DynamicInvocation);
                                                            var downLevelExpression = invocationOperation?.Parent;

                                                            var topLevelExpressions = operations.Where(o => o.Kind == OperationKind.ExpressionStatement && o.Parent == null && o != downLevelExpression);
                                                            var argumentOperations = operations.Where(o => o.Kind == OperationKind.Argument);
                                                            
                                                            var topLevelSinkOperation = topLevelExpressions.FirstOrDefault(o => o.Syntax.GetLocation().SourceSpan.OverlapsWith(sourceSink.Sink.Location.SourceSpan));
                                                            if (topLevelSinkOperation != null)
                                                            {
                                                                var sinkParameterVariable = topLevelSinkOperation.Descendants().FirstOrDefault(o => o.Kind == OperationKind.LocalReference || o.Kind == OperationKind.ParameterReference).Syntax.GetText().ToString();
                                                                // catching simple assignments only for now
                                                                var assignementOps = operations.Where(o => o.Kind == OperationKind.SimpleAssignment);
                                                                foreach (var assignementOp in assignementOps)
                                                                {
                                                                    var assignmentParameterText = assignementOp.Children.FirstOrDefault(o => o.Kind == OperationKind.LocalReference).Syntax.GetText().ToString();
                                                                    if (assignmentParameterText.StartsWith(sinkParameterVariable)) // how to eliminate possible doppelgangers?
                                                                    {
                                                                        var sourceNodeText = sourceLocation.SourceTree.GetRoot().FindNode(sourceLocation.SourceSpan).GetText().ToString();
                                                                        // sink -> simple assignment
                                                                        var parameterReferenceOp = assignementOp.Descendants().FirstOrDefault(o => o.Kind == OperationKind.ParameterReference); // is is a source?
                                                                        var parameterReferenceName = parameterReferenceOp.Syntax.GetText().ToString();
                                                                        var sourceArray = sourceNodeText.Split(' '); // is there a way to have more than 2 output based on number of spaces?
                                                                        var sourceParameterName = sourceArray[sourceArray.Length - 1];
                                                                        if (parameterReferenceName == sourceParameterName)
                                                                        {
                                                                            // source found
                                                                            taintedOperations.Add(assignementOp);                                                                            
                                                                        }
                                                                        else
                                                                        {
                                                                            // need to go up - another assignment, what is our source?
                                                                        }
                                                                        
                                                                        // it should be "one way" only from sink to source, can break now
                                                                        break;
                                                                    }
                                                                }

                                                                taintedOperations.Add(topLevelSinkOperation);
                                                                return;
                                                            }

                                                            // add results from the current method
                                                            taintedOperations.AddRange(operations.Where(i => i.Parent == null).Except(topLevelExpressions));

                                                            if (taintedResult.InterproceduralResultAvailable())
                                                            {
                                                                // invocationOperation should not be null there, topLevelSinkOperation should be null and vice versa
                                                                var r = taintedResult.TryGetInterproceduralResult(invocationOperation);
                                                                if (r != null)
                                                                {
                                                                    var taintedOps = r.GetTaintedOperations();
                                                                    // we need to pass a new "source"
                                                                    var invocationParameterOp = taintedOps.FirstOrDefault(o => o.Kind == OperationKind.Argument && o.Parent.Kind == OperationKind.Invocation);
                                                                    //var invocationParameterOp = invocationOperation.Descendants().FirstOrDefault(c => c.Kind == OperationKind.InstanceReference);
                                                                    AddCurrentLevelResults(taintedOps, r, sourceSink, invocationParameterOp.Syntax.GetLocation());
                                                                }
                                                            }

                                                        }

                                                        // prepare a list of addional locations starting from the source
                                                        var additionalLocations = new Location[taintedOperations.Count + 1];
                                                        additionalLocations[0] = sourceOrigin.Location;
                                                        var sb = new StringBuilder();

                                                        for (int i = 0; i < taintedOperations.Count; i++)
                                                        {
                                                            additionalLocations[i + 1] = taintedOperations[i].Syntax.GetLocation();
                                                            sb.AppendLine(taintedOperations[i].Syntax.ToFullString());
                                                        }

                                                        var messageArgs = new object[4 + additionalLocations.Length];
                                                        messageArgs[0] = sourceSink.Sink.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                                                        messageArgs[1] = sourceSink.Sink.AccessingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                                                        messageArgs[2] = sourceOrigin.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                                                        messageArgs[3] = sourceOrigin.AccessingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                                                        for (int i = 0; i < additionalLocations.Length; i++)
                                                        {
                                                            messageArgs[i + 4] = additionalLocations[i].SourceTree.GetText().GetSubText(additionalLocations[i].SourceSpan).ToString();
                                                        }                


                                                        var dd = new DiagnosticDescriptor(
                                                            TaintedDataEnteringSinkDescriptor.Id,
                                                            TaintedDataEnteringSinkDescriptor.Title,
                                                            TaintedDataEnteringSinkDescriptor.MessageFormat,
                                                            TaintedDataEnteringSinkDescriptor.Category,
                                                            TaintedDataEnteringSinkDescriptor.DefaultSeverity,
                                                            TaintedDataEnteringSinkDescriptor.IsEnabledByDefault,
                                                            sb.ToString());

                                                        // Something like:
                                                        // CA3001: Potential SQL injection vulnerability was found where '{0}' in method '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                                        // Message format is applicable only to the Title. It is hard to read a very long Title in the ErrorList, use Description instead.
                                                        // Description can be accessed in the ErrorList output by expanding an entry. Roslyn for VS doesn't provide access to it in the "detailsexpander".
                                                        // Sad face here: https://github.com/dotnet/roslyn/blob/main/src/VisualStudio/Core/Def/Implementation/TableDataSource/VisualStudioDiagnosticListTable.BuildTableDataSource.cs#L137-L209
                                                        // We need to save the full diagnostic somewhere else or use compilation.WithAnalyzers in workspace.Solution.Projects. CompilationWithAnalyzers will create a new compilation object, it looks like we can't retrive cached compilation with our analyzer after the build.
                                                        // Analyzer in the nuget package is invoked on the build. Analyzer in the VS extension will need to use "Analyze" button (Alt + F11).
                                                        Diagnostic diagnostic = Diagnostic.Create(
                                                            dd,
                                                            sourceSink.Sink.Location,
                                                            additionalLocations: additionalLocations,
                                                            messageArgs: messageArgs);
                                                        operationBlockAnalysisContext.ReportDiagnostic(diagnostic);

                                                        DiagnosticResults.AddDiagnostic(diagnostic);
                                   
                                                    }

                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        rootOperationsNeedingAnalysis.Free(compilationContext.CancellationToken);
                                    }
                                });
                        });
                });
        }
    }

}