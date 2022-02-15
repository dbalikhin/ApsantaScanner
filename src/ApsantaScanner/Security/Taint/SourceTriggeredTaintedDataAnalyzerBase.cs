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
            var s = "di";
            Console.WriteLine(s);
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


                                                    foreach (SymbolAccess sourceOrigin in sourceSink.SourceOrigins)
                                                    {
                                                        var initialTaintedOperations = taintedDataAnalysisResult.GetTaintedOperations();
                                                        List<IOperation> taintedOperations = new();
                                                        AddCurrentLevelResults(initialTaintedOperations, taintedDataAnalysisResult);



                                                        void AddCurrentLevelResults(List<IOperation> operations, DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> taintedResult)
                                                        {
                                                            // add results from the current method
                                                            taintedOperations.AddRange(operations.Where(i => i.Parent == null));
                                                            // assume there is only a single invocation in a method that leads from the specific source to the sink
                                                            var invocation = operations.FirstOrDefault(op => op.Kind is OperationKind.Invocation or OperationKind.DynamicInvocation);
                                                            if (invocation == null)
                                                            {
                                                                // need to exit earlier?
                                                            }

                                                            if (taintedResult.InterproceduralResultAvailable())
                                                            {
                                                                var r = taintedResult.TryGetInterproceduralResult(invocation);
                                                                if (r != null)
                                                                {
                                                                    var taintedOps = r.GetTaintedOperations();

                                                                    AddCurrentLevelResults(taintedOps, r);
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

               
                                                        // multiple arguments for additional locations but it is unclear how to use in a static message file
                                                        // messageArgs[i + 4] = additionalLocations[i].SourceTree.GetText().GetSubText(additionalLocations[i].SourceSpan).ToString();


                                                        var dd = new DiagnosticDescriptor(TaintedDataEnteringSinkDescriptor.Id, TaintedDataEnteringSinkDescriptor.Title, TaintedDataEnteringSinkDescriptor.MessageFormat, TaintedDataEnteringSinkDescriptor.Category, TaintedDataEnteringSinkDescriptor.DefaultSeverity, TaintedDataEnteringSinkDescriptor.IsEnabledByDefault, sb.ToString());

                                                        // Something like:
                                                        // CA3001: Potential SQL injection vulnerability was found where '{0}' in method '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                                        Diagnostic diagnostic = Diagnostic.Create(
                                                            dd,
                                                            sourceSink.Sink.Location,
                                                            additionalLocations: additionalLocations,
                                                            messageArgs: messageArgs);
                                                        operationBlockAnalysisContext.ReportDiagnostic(diagnostic);

                                                        /*
                                                        string path = @"c:\temp\MyTest.txt";
                                                        // This text is added only once to the file.
                                                        if (!File.Exists(path))
                                                        {
                                                            // Create a file to write to.
                                                            using (StreamWriter sw = File.CreateText(path))
                                                            {
                                                                sw.WriteLine(TaintedDataEnteringSinkDescriptor.Id);
                                                                sw.WriteLine(TaintedDataEnteringSinkDescriptor.Category);
                                                                sw.WriteLine(TaintedDataEnteringSinkDescriptor.Description);
                                                                sw.WriteLine();
                                                            }
                                                        }

                                                        // This text is always added, making the file longer over time
                                                        // if it is not deleted.
                                                        using (StreamWriter sw = File.AppendText(path))
                                                        {
                                                            sw.WriteLine(TaintedDataEnteringSinkDescriptor.Id);
                                                            sw.WriteLine(TaintedDataEnteringSinkDescriptor.Category);
                                                            sw.WriteLine(TaintedDataEnteringSinkDescriptor.Description);
                                                            sw.WriteLine();
                                                        }
                                                        */

                                                        //MultiThreadFileWriter.Instance.WriteLine(sb.ToString());
                                                        DiagnosticResults.AddDiagnostic(diagnostic);
                                                        DiagnosticResults.WriteToFile();

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